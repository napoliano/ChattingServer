using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public class Server
    {
        private readonly ListenSocket _listenSocket = new();


        public bool Start()
        {
            InitializeManager();

            if (_listenSocket.Start(7001) == false)
            {
                return false;
            }

            while (true)
            {
                var cmd = Console.ReadLine();

                if (string.Equals(cmd, "/exit", StringComparison.OrdinalIgnoreCase))
                {
                    Stop();
                    
                    break;
                }
            }

            return true;
        }

        private void InitializeManager()
        {
            ProtoMessageTypeManager.Instance.Initialize();

            PacketHandlerManager.Instance.Initialize();

            ChatRoomGroupManager.Instance.Initialize();
        }

        public void Stop()
        {
            _listenSocket.Dispose();
        }
    }
}
