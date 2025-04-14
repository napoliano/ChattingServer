using System;
using System.Net;
using System.Net.Sockets;


namespace Server
{
    public class ListenSocket
    {
        private readonly Socket _listenSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly SocketAsyncEventArgs _acceptEventArgs = new();

        private bool _isRunning = true;


        public bool Start(int port)
        {
            try
            {
                _listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                _listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _listenSocket.Listen(GlobalConstants.Network.Backlog);

                _acceptEventArgs.Completed += OnAcceptCompleted;

                StartAccept(_acceptEventArgs);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to listen - port:{port}");
                return false;
            }

            return true;
        }

        private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (_isRunning == false)
                return;

            try
            {
                if (_listenSocket.AcceptAsync(e) == false)
                    ProcessAccept(e);
            }
            //소켓이 dispose된 경우
            catch (ObjectDisposedException) 
            {
                Log.Debug($"ListenSocket disposed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"AcceptAsync failed - SocketError:{e.SocketError}");

                //Accept 재시도
                StartAccept(e);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (_isRunning == false)
                return;

            if ((e.SocketError != SocketError.Success) || (e.AcceptSocket == null))
            {
                Log.Error($"Failed to accept client - SocketError:{e.SocketError}");
            }
            else
            {
                var session = new ClientSession(e.AcceptSocket);
                ClientSessionManager.Instance.AddSession(session);

                session.StartReceive();
            }

            e.AcceptSocket = null;

            StartAccept(e);
        }

        public void Stop()
        {
            if (_isRunning == false)
                return;

            _isRunning = false;

            try
            {
                _listenSocket.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to close socket");
            }

            _acceptEventArgs.Completed -= OnAcceptCompleted;
            _acceptEventArgs.Dispose();
        }
    }
}
