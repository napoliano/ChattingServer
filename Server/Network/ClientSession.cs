using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ProtoBuf;


namespace Server
{
    public class ClientSession
    {
        private static int IdCounter;

        public int Id => _id;
        private readonly int _id;

        public User User => _user;
        private readonly User _user;

        private readonly Socket _socket;

        private readonly SocketAsyncEventArgsEx _receiveEventArgsEx;
        private readonly SocketAsyncEventArgsEx _sendEventArgsEx;

        private readonly Queue<SendPacket> _sendQueue = new();

        private readonly SingleConsumerChannel _channel = new();

        private int _sessionState = GlobalConstants.SessionState.Connected;


        public ClientSession(Socket socket)
        {
            _id = Interlocked.Increment(ref IdCounter);

            _user = new User(this);

            _socket = socket;

            _receiveEventArgsEx = ObjectPool<SocketAsyncEventArgsEx>.Rent();
            _sendEventArgsEx = ObjectPool<SocketAsyncEventArgsEx>.Rent();

            _receiveEventArgsEx.SAEA.SetBuffer(_receiveEventArgsEx.Buffer, 0, GlobalConstants.Network.MaxPacketSize);
            _receiveEventArgsEx.SAEA.Completed += OnReceiveCompleted;

            _sendEventArgsEx.SAEA.SetBuffer(_sendEventArgsEx.Buffer, 0, GlobalConstants.Network.MaxPacketSize);
            _sendEventArgsEx.SAEA.Completed += OnSendCompleted;
        }

        public void StartReceive()
        {
            StartReceive(_receiveEventArgsEx.SAEA);
        }

        private void StartReceive(SocketAsyncEventArgs e)
        {
            if (_sessionState == GlobalConstants.SessionState.Disconnected)
                return;

            bool pending = false;
            try
            {
                pending = _socket.ReceiveAsync(e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ReceiveAsync failed - Id:{_id}, SocketError:{e.SocketError}");
                CloseSession();
            }

            if (pending == false)
            {
                OnReceiveCompleted(this, e);
            }
        }

        private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (ProcessReceive(e) == false)
            {
                if ((e.BytesTransferred > 0) || (e.SocketError != SocketError.Success))
                    Log.Error($"ProcessReceive failed - Id:{_id}, bytesTransferred:{e.BytesTransferred}, socketError:{e.SocketError}");

                CloseSession();
            }
        }

        private bool ProcessReceive(SocketAsyncEventArgs e)
        {
            if ((e.BytesTransferred <= 0) || (e.SocketError != SocketError.Success))
                return false;

            if (ParsePacket(e) == false)
            {
                Log.Error($"ParsePacket failed - Id:{_id}");
                return false;
            }

            StartReceive(e);

            return true;
        }

        private bool ParsePacket(SocketAsyncEventArgs e)
        {
            int remainBytes = e.BytesTransferred + e.Offset;
            int parseBytes = 0;

            while (true)
            {
                if (remainBytes < sizeof(short))
                    break;

                int packetSize = MemoryMarshal.Read<short>(e.Buffer.AsSpan(parseBytes));
                if ((packetSize < PacketHeader.HeaderSize) || (packetSize > GlobalConstants.Network.MaxPacketSize))
                {
                    Log.Error($"Invalid packet size - Id:{_id}, packetSize:{packetSize}");
                    return false;
                }

                if (packetSize > remainBytes)
                    break;

                if (ProcessPacket(e.Buffer, parseBytes, packetSize) == false)
                {
                    Log.Error($"ProcessPacket failed - Id:{_id}");
                    return false;
                }

                parseBytes += packetSize;
                remainBytes -= packetSize;
            }

            if ((remainBytes > 0) && (parseBytes > 0))
                e.Buffer.AsSpan(parseBytes, remainBytes).CopyTo(e.Buffer);

            e.SetBuffer(remainBytes, GlobalConstants.Network.MaxPacketSize - remainBytes);

            return true;
        }

        private bool ProcessPacket(byte[] buffer, int offset, int packetSize)
        {
            var command = (PacketCommand)MemoryMarshal.Read<short>(buffer.AsSpan(offset + sizeof(short)));
            var type = ProtoMessageTypeManager.Instance.TryGetType(command);
            if (type == null)
            {
                Log.Error($"Unknown packet command - Id:{_id}, command:{command}");
                return false;
            }

            object message;
            try
            {
                using var stream = new MemoryStream(buffer, offset, packetSize);
                message = Serializer.Deserialize(type, stream);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Deserialize fail - Id:{_id}, command:{command}, offset:{offset}, packetSize:{packetSize}");
                return false;
            }

            TryWriteToChannel(async () =>
            {
                await PacketHandlerManager.Instance.HandleAsync(command, this, message);
            });

            return true;
        }


        public void SendResponse<T>(PacketCommand command, T message) where T : class, IExtensible
        {
            if (_sessionState == GlobalConstants.SessionState.Disconnected)
                return;

            var packet = PacketSerializer.MakeSendPacket(command, message);

            TryWriteToChannel(async () =>
            {
                _sendQueue.Enqueue(packet);

                if (_sendQueue.Count == 1)
                    StartSend(_sendEventArgsEx.SAEA);

                await Task.CompletedTask;
            });
        }

        private void StartSend(SocketAsyncEventArgs e)
        {
            bool pending = false;
            try
            {
                var packet = _sendQueue.Peek();

                packet.Buffer.AsSpan(0, packet.PacketSize).CopyTo(e.Buffer);
                e.SetBuffer(0, packet.PacketSize);

                pending = _socket.SendAsync(e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"SendAsync failed - Id:{Id}");
                CloseSession();
            }

            if (pending == false)
            {
                OnSendCompleted(this, e);
            }
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            TryWriteToChannel(async () =>
            {
                ProcessSend(e);

                await Task.CompletedTask;
            });
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if ((e.BytesTransferred <= 0) || (e.SocketError != SocketError.Success))
            {
                Log.Error($"Send error - Id:{_id}, bytesTransferred:{e.BytesTransferred}, socketError:{e.SocketError}");
                CloseSession();
                return;
            }

            var packet = _sendQueue.Peek();
            int sentBytes = e.BytesTransferred + e.Offset;

            if (packet.PacketSize > sentBytes)
            {
                e.SetBuffer(sentBytes, packet.PacketSize - sentBytes);

                try
                {
                    if (_socket.SendAsync(e) == false)
                        OnSendCompleted(this, e);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"SendAsync failed - Id:{_id}");
                    CloseSession();
                }

                return;
            }

            var sendPacket = _sendQueue.Peek();
            _sendQueue.Dequeue();

            ObjectPool<SendPacket>.Return(sendPacket);

            if (_sendQueue.Count > 0)
            {
                StartSend(e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryWriteToChannel(Func<Task> func)
        {
            _channel.TryWrite(new SessionChannelItem(func));
        }

        public void CloseSession()
        {
            int originalSessionState = Interlocked.Exchange(ref _sessionState, GlobalConstants.SessionState.Disconnected);
            if (originalSessionState == GlobalConstants.SessionState.Disconnected)
                return;

            //채널 종료
            _channel.Close();

            ClientSessionManager.Instance.TryRemoveSession(_id);

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Shutdown fail - Id:{_id}");
            }
            finally
            {
                _socket.Close();
            }

            _receiveEventArgsEx.SAEA.Completed -= OnReceiveCompleted;
            _sendEventArgsEx.SAEA.Completed -= OnSendCompleted;

            ObjectPool<SocketAsyncEventArgsEx>.Return(_receiveEventArgsEx);
            ObjectPool<SocketAsyncEventArgsEx>.Return(_sendEventArgsEx);

            foreach (var sendPacket in _sendQueue)
            {
                ObjectPool<SendPacket>.Return(sendPacket);
            }
        }
    }
}
