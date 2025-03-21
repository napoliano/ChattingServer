using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Akka.Actor;
using Akka.Event;
using UserPacket;


namespace ChattingServer
{
    public partial class UserSocketActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private static IActorRef _userSocketManager;

        private readonly int _id;

        private readonly Socket _socket;

        //[Todo] SocketAsyncEventArgsEx 풀링해서 사용할 것
        private readonly SocketAsyncEventArgsEx _receiveEventArgsEx = new();
        private readonly SocketAsyncEventArgsEx _sendEventArgsEx = new();

        private readonly Queue<SendPacket> _sendPacketQueue = new();

        public IActorRef User => _user;
        private readonly IActorRef _user;

        private readonly IActorRef _self;


        public static Props Props(int id, Socket socket) => Akka.Actor.Props.Create(() => new UserSocketActor(id, socket));

        public UserSocketActor(int id, Socket socket)
        {
            _id = id;
            _socket = socket;

            _user = Context.ActorOf(UserActor.Props(_id, Self), $"{nameof(UserActor)}_{_id}");

            _self = Self;


            Receive<StartReceive>(message =>
            {
                _receiveEventArgsEx.SAEA.SetBuffer(_receiveEventArgsEx.Buffer, 0, SocketAsyncEventArgsEx.PacketBufferSize);
                _receiveEventArgsEx.SAEA.Completed += OnReceiveCompleted;

                _sendEventArgsEx.SAEA.SetBuffer(_sendEventArgsEx.Buffer, 0, SocketAsyncEventArgsEx.PacketBufferSize);
                _sendEventArgsEx.SAEA.Completed += OnSendCompleted;

                StartReceive(_receiveEventArgsEx.SAEA);
            });

            Receive<ProcessReceive>(message => ProcessReceive(message.EventArgs));

            Receive<ProcessSend>(message => ProcessSend(message.EventArgs));

            Receive<SendResponse>(message => Send(new SendPacket(message.Buffer, message.PacketSize)));
        }

        private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            _self.Tell(new ProcessReceive(e));
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if ((e.BytesTransferred <= 0) || (e.SocketError != SocketError.Success))
            {
                CloseSocket();
                return;
            }

            ParsePacket(e);

            StartReceive(e);
        }

        private void StartReceive(SocketAsyncEventArgs e)
        {
            bool pending = false;
            try
            {
                pending = _socket.ReceiveAsync(e);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"ReceiveAsync failed - Id:{_id}, SocketError:{e.SocketError}");
                CloseSocket();

                return;
            }

            if (pending == false)
            {
                _self.Tell(new ProcessReceive(e));
            }
        }

        private void ParsePacket(SocketAsyncEventArgs e)
        {
            int remainBytes = e.BytesTransferred + e.Offset;
            int parseBytes = 0;

            while (true)
            {
                if (remainBytes < PacketHeader.SizeSize)
                    break;

                int packetSize = MemoryMarshal.Read<short>(e.Buffer.AsSpan(parseBytes));
                if ((packetSize < PacketHeader.HeaderSize) || (packetSize > SocketAsyncEventArgsEx.PacketBufferSize))
                {
                    _log.Error($"Invalid packet size - Id:{_id}, PacketSize:{packetSize}");
                    CloseSocket();
                    return;
                }

                if (packetSize > remainBytes)
                    break;

                if (ProcessPacket(e.Buffer, parseBytes, packetSize) == false)
                {
                    _log.Error($"ProcessPacket failed - Id:{_id}");
                    CloseSocket();
                    return;
                }

                parseBytes += packetSize;
                remainBytes -= packetSize;
            }

            if (remainBytes == 0)
            {
                e.SetBuffer(0, SocketAsyncEventArgsEx.PacketBufferSize);
            }
            else
            {
                if (parseBytes > 0)
                    e.Buffer.AsSpan(parseBytes, remainBytes).CopyTo(e.Buffer);

                e.SetBuffer(remainBytes, SocketAsyncEventArgsEx.PacketBufferSize - remainBytes);
            }
        }

        private bool ProcessPacket(byte[] buffer, int offset, int packetBytes)
        {
            var command = (PacketCommand)MemoryMarshal.Read<short>(buffer.AsSpan(offset + PacketHeader.SizeSize));
            
            var protobufMessage = ProtobufMessageParserManager.TryParse(command, buffer, offset + PacketHeader.HeaderSize, packetBytes - PacketHeader.HeaderSize);
            if (protobufMessage == null)
            {
                _log.Error($"Failed to parse packet - Id:{_id}, Command:{command}");
                return false;
            }

            if (PacketHandlerManager.TryHandle(command, this, protobufMessage) == false)
            {
                _log.Error($"Failed to handle packet - Id:{_id}, Command:{command}");
                return false;
            }

            return true;
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            _self.Tell(new ProcessSend(e));
        }

        private void Send(SendPacket packet)
        {
            bool empty = (_sendPacketQueue.Count == 0);
            _sendPacketQueue.Enqueue(packet);

            if (empty)
            {
                StartSend(_sendEventArgsEx.SAEA);
            }
        }
        
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if ((e.BytesTransferred <= 0) || (e.SocketError != SocketError.Success))
            {
                CloseSocket();
                return;
            }

            var packet = _sendPacketQueue.Peek();
            int sentBytes = e.BytesTransferred + e.Offset;

            if (packet.PacketSize > sentBytes)
            {
                e.SetBuffer(sentBytes, packet.PacketSize - sentBytes);

                bool pending = false;
                try
                {
                    pending = _socket.SendAsync(e);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"ReceiveAsync failed - Id:{_id}, SocketError:{e.SocketError}");
                    CloseSocket();

                    return;
                }

                if (pending == false)
                {
                    OnSendCompleted(this, e);
                }

                return;
            }

            _sendPacketQueue.Dequeue();

            if (_sendPacketQueue.Count > 0)
            {
                StartSend(e);
            }
        }

        private void StartSend(SocketAsyncEventArgs e)
        {
            var packet = _sendPacketQueue.Peek();
            int packetBytes = packet.PacketSize;

            bool pending = false;
            try
            {
                packet.Buffer.AsSpan(0, packetBytes).CopyTo(e.Buffer);
                e.SetBuffer(0, packetBytes);

                pending = _socket.SendAsync(e);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"ReceiveAsync failed - Id:{_id}, SocketError:{e.SocketError}");
                CloseSocket();

                return;
            }

            if (pending == false)
            {
                OnSendCompleted(this, e);
            }
        }

        private void CloseSocket()
        {
            _userSocketManager.Tell(new RemoveUserSocket(_id));
        }

        public static void SetUserSocketManager(IActorRef userSocketManager)
        {
            _userSocketManager = userSocketManager;
        }

        protected override void PostStop()
        {
            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            finally
            {
                _socket.Close();
                _socket.Dispose();
            }

            _receiveEventArgsEx.SAEA.Completed -= OnReceiveCompleted;
            _sendEventArgsEx.SAEA.Completed -= OnSendCompleted;

            _receiveEventArgsEx.SAEA.Dispose();
            _sendEventArgsEx.SAEA.Dispose();

            Context.Stop(_user);
        }
    }
}
