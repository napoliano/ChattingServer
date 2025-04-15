using System;
using System.Net;
using System.Net.Sockets;


namespace Server
{
    public class ListenSocket : IDisposable
    {
        private readonly Socket _listenSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly SocketAsyncEventArgs _acceptEventArgs = new();

        private bool _disposed = false;

        //Accept를 연속으로 재시도한 횟수 관리
        private RetryThresholdCounter _acceptRetryCounter;


        public ListenSocket()
        {
            _acceptRetryCounter = new(GlobalConstants.Network.MaxAcceptRetryCount, OnAcceptThresholdExceeded);
        }

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
                Log.Error(ex, $"Start failed - port:{port}");
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
            if (_disposed)
                return;

            try
            {
                if (_listenSocket.AcceptAsync(e) == false)
                    ProcessAccept(e);

                _acceptRetryCounter.Reset();
            }
            //소켓이 dispose된 경우
            catch (ObjectDisposedException) 
            {
                Log.Debug($"AcceptAsync aborted");
            }
            catch (Exception ex)
            {
                _acceptRetryCounter.AddCount();

                //Accept 재시도
                StartAccept(e);

                Log.Error(ex, $"AcceptAsync failed - socketError:{e.SocketError}, retryCount:{_acceptRetryCounter.RetryCount}");
            }
        }

        /// <summary>
        /// 연달은 Accept 재시도 한계 초과 시 호출되는 콜백
        /// </summary>
        private void OnAcceptThresholdExceeded()
        {
            //[Todo] 팀 정책에 따라 구현
            //ex. 서버 종료, 알람 메일 전송 등..
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (_disposed)
                return;

            if ((e.SocketError != SocketError.Success) || (e.AcceptSocket == null))
            {
                Log.Error($"Client accept failed - socketError:{e.SocketError}");
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _listenSocket.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ListenSocket close failed");
            }

            _acceptEventArgs.Completed -= OnAcceptCompleted;
            _acceptEventArgs.Dispose();
        }

        ~ListenSocket()
        {
            Dispose(false);
        }
    }
}
