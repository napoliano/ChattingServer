using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public static class GlobalConstants
    {
        public static class ChatRoom
        {
            public static readonly int MaxChatRoomGroupCount = 10;

            public static readonly int MaxChatRoomCountByGroup = 128;

            public static readonly string SystemName = "System";
        }

        public static class Network
        {
            public static readonly int MaxPacketSize = 8192;

            public static readonly int MaxObjectPoolSize = 1024;

            public static readonly string PacketHandlerPrefix = "On";

            public static readonly string ProtoMessagePrefix = "Cs";
        }
        
        public static class SessionState
        {
            public static readonly int Connected = 1;
            public static readonly int Disconnected = 0;
        }

        public static class Time
        {
            public static readonly int OneSecondMs = 1000;
        }
    }
}
