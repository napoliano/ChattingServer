using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class GlobalConstants
    {
        public static readonly int MaxPacketSize = 8192;

        public static readonly string PacketHandlerPrefix = "On";

        public static readonly string ProtoMessagePrefix = "Cs";
    }
}
