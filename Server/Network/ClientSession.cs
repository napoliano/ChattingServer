using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ProtoBuf;


namespace Server
{
    public class ClientSession
    {
        private static int s_id;

        public int Id => _id;
        private readonly int _id;

        public User User => _user;
        private readonly User _user;

        private readonly Socket _socket;

        private readonly SocketAsyncEventArgsEx _receiveEventArgsEx;
        private readonly SocketAsyncEventArgsEx _sendEventArgsEx;

        private readonly Queue<SendPacket> _sendQueue = new();
        private readonly Channel<Action> _eventChannel = Channel.CreateUnbounded<Action>();
        private readonly CancellationTokenSource _cancellationToken = new();

        private int _sessionState = GlobalConstants.SessionState.Connected;


        public ClientSession(Socket socket)
        {
            _id = Interlocked.Increment(ref s_id);

            _user = new User(this);

            _socket = socket;

            _receiveEventArgsEx = ObjectPool<SocketAsyncEventArgsEx>.Rent();
            _sendEventArgsEx = ObjectPool<SocketAsyncEventArgsEx>.Rent();

            _receiveEventArgsEx.SAEA.SetBuffer(_receiveEventArgsEx.Buffer, 0, GlobalConstants.Network.MaxPacketSize);
            _receiveEventArgsEx.SAEA.Completed += OnReceiveCompleted;

            _sendEventArgsEx.SAEA.SetBuffer(_sendEventArgsEx.Buffer, 0, GlobalConstants.Network.MaxPacketSize);
            _sendEventArgsEx.SAEA.Completed += OnSendCompleted;

            _ = RunEventLoopAsync();
        }

        public void StartReceive()
        {
            StartReceive(_receiveEventArgsEx.SAEA);
        }

        private void StartReceive(SocketAsyncEventArgs e)
        {
            if (_sessionState == GlobalConstants.SessionState.Disconnected)
                return;

            try
            {
                if (_socket.ReceiveAsync(e) == false)
                    ProcessReceive(e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ReceiveAsync failed - Id:{_id}, SocketError:{e.SocketError}");
                CloseSession();
            }
        }

        private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0)
            {
                CloseSession();
                return;
            }

            if (e.SocketError != SocketError.Success)
            {
                Log.Error($"Receive error - Id:{_id}, SocketError:{e.SocketError}");
                CloseSession();
                return;
            }

            if (ParsePacket(e) == false)
            {
                Log.Error($"ParsePacket failed - Id:{_id}");
                CloseSession();
                return;
            }

            StartReceive(e);
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
                if ((packetSize < PacketHeader.HeaderSize) || (packetSize > PacketBase.MaxPacketSize))
                {
                    Log.Error($"Invalid packet size - Id:{_id}, {nameof(packetSize)}:{packetSize}");
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
                Log.Error($"Unknown packet command - Id:{_id}, {nameof(command)}:{command}");
                return false;
            }

            try
            {
                using var stream = new MemoryStream(buffer, offset, packetSize);
                var message = ProtoBuf.Serializer.Deserialize(type, stream);

                PacketHandlerManager.Instance.Handle(command, this, message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to deserialize packet - Id:{_id}, {nameof(command)}:{command}, {nameof(packetSize)}:{packetSize}");
                return false;
            }

            return true;
        }

        public void SendResponse<T>(PacketCommand command, T message) where T : class, IExtensible
        {
            if (_sessionState == GlobalConstants.SessionState.Disconnected)
                return;

            var packet = PacketSerializer.MakeSendPacket(command, message);

            _eventChannel.Writer.TryWrite(() =>
            {
                bool isEmpty = _sendQueue.Count == 0;
                _sendQueue.Enqueue(packet);

                if (isEmpty)
                    StartSend(_sendEventArgsEx.SAEA);
            });
        }

        private void StartSend(SocketAsyncEventArgs e)
        {
            try
            {
                var packet = _sendQueue.Peek();

                packet.Buffer.AsSpan(0, packet.PacketSize).CopyTo(e.Buffer);
                e.SetBuffer(0, packet.PacketSize);

                if (_socket.SendAsync(e) == false)
                    _eventChannel.Writer.TryWrite(() => ProcessSend(e));
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"SendAsync failed - Id:{Id}");
                CloseSession();
            }
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            _eventChannel.Writer.TryWrite(() => ProcessSend(e));
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if ((e.BytesTransferred <= 0) || (e.SocketError != SocketError.Success))
            {
                Log.Error($"Send error - Id:{_id}, BytesTransferred:{e.BytesTransferred}, SocketError:{e.SocketError}");
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

        private async Task RunEventLoopAsync()
        {
            try
            {
                await foreach (var action in _eventChannel.Reader.ReadAllAsync(_cancellationToken.Token))
                    action?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"RunEventLoopAsync failed - Id:{_id}");
                CloseSession();
            }
        }

        public void CloseSession()
        {
            _sessionState = Interlocked.Exchange(ref _sessionState, GlobalConstants.SessionState.Disconnected);
            if (_sessionState == GlobalConstants.SessionState.Disconnected)
                return;

            //채널 및 이벤트 루프 종료
            _eventChannel.Writer.Complete();
            _cancellationToken.Cancel();

            ClientSessionManager.Instance.TryRemoveSession(_id);


            //자원 정리
            _ = Task.Run(async () =>
            {
                await Task.Delay(GlobalConstants.Time.OneSecondMs);

                try
                {
                    if (_socket.Connected)
                        _socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to shutdown socket of session - Id:{_id}");
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
            });
        }
    }
}
