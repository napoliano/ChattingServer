using System;
using System.Net;
using System.Net.Sockets;
using Akka.Actor;
using Akka.Event;


namespace ChattingServer
{
    public class ChatServerActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly Socket _listenSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly SocketAsyncEventArgs _acceptEventArgs = new();

        private readonly IActorRef _userSocketManager;

        private readonly IActorRef _chatRoomManager;

        private readonly IActorRef _self;

        private bool _stopServer;


        public static readonly Props Props = Props.Create(() => new ChatServerActor());

        public ChatServerActor()
        {
            _userSocketManager = Context.ActorOf(UserSocketManagerActor.Props, nameof(UserSocketManagerActor));
            UserSocketActor.SetUserSocketManager(_userSocketManager);

            _chatRoomManager = Context.ActorOf(ChatRoomManagerActor.Props, nameof(ChatRoomManagerActor));
            UserActor.SetChatRoomManager(_chatRoomManager);
            ChatRoomActor.SetChatRoomManager(_chatRoomManager);

            _self = Self;


            Receive<StartServer>(message =>
            {
                try
                {
                    _listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    _listenSocket.Bind(new IPEndPoint(IPAddress.Any, message.Port));
                    _listenSocket.Listen();

                    _acceptEventArgs.Completed += OnAcceptCompleted;

                    StartAccept(_acceptEventArgs);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Failed to start server - port:{message.Port}");
                }
            });

            Receive<ProcessAccept>(message =>
            {
                ProcessAccept(message.EventArgs);
            });
        }

        private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
        {
            _self.Tell(new ProcessAccept(e));
        }

        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (_stopServer)
                return;

            bool pending = false;
            try
            {
                pending = _listenSocket.AcceptAsync(e);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"AcceptAsync failed - SocketError:{e.SocketError}");

                //Accept 재시도
                StartAccept(e);

                return;
            }

            if (pending == false)
            {
                _self.Tell(new ProcessAccept(e));
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (_stopServer)
                return;

            if ((e.SocketError != SocketError.Success) || (e.AcceptSocket == null))
            {
                _log.Error($"Failed to accept client - SocketError:{e.SocketError}");
            }
            else
            {
                _userSocketManager.Tell(new AddUserSocket(e.AcceptSocket));
            }

            e.AcceptSocket = null;

            StartAccept(e);
        }

        protected override void PostStop()
        {
            if (_stopServer)
                return;

            _stopServer = true;

            try
            {
                if (_listenSocket.Connected)
                {
                    _listenSocket.Shutdown(SocketShutdown.Both);
                }
            }
            finally
            {
                _listenSocket.Close();
                _listenSocket.Dispose();
            }

            _acceptEventArgs.Completed -= OnAcceptCompleted;
            _acceptEventArgs.Dispose();
        }
    }
}
