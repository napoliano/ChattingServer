using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChattingServer
{
    public class SocketAsyncEventArgsEx
    {
        public static readonly int PacketBufferSize = 8192;

        public SocketAsyncEventArgs SAEA { get; set; } = new();

        public byte[] Buffer { get; set; } = new byte[PacketBufferSize];
    }
}
