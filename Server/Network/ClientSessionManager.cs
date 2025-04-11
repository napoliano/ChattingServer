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


        public void AddSession(ClientSession session)
        {
            Monitor.Enter(_lock);
            _sessions[session.Id] = session;
            Monitor.Exit(_lock);
        }

        public bool TryRemoveSession(int userSocketId)
        {
            Monitor.Enter(_lock);
            bool result = _sessions.Remove(userSocketId);
            Monitor.Exit(_lock);

            return result;
        }

        public bool TryGetSession(int sessionId, out ClientSession? session)
        {
            Monitor.Enter(_lock);
            bool result = _sessions.TryGetValue(sessionId, out session);
            Monitor.Exit(_lock);

            return result;
        }
    }
}
