using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Server
{
    public class ClientSessionManager : Singleton<ClientSessionManager>
    {
        private readonly Dictionary<int, ClientSession> _sessions = new();
        private readonly object _lock = new();


        public void AddSession(in ClientSession session)
        {
            lock (_lock)
            {
                _sessions[session.Id] = session;
            }
        }

        public bool TryRemoveSession(int userSocketId)
        {
            lock (_lock)
            {
                return _sessions.Remove(userSocketId);
            }
        }

        public bool TryGetSession(int sessionId, out ClientSession? session)
        {
            lock (_lock)
            {
                return _sessions.TryGetValue(sessionId, out session);
            }
        }
    }
}
