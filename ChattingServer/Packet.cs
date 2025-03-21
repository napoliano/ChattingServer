using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChattingServer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PacketHeader
    {
        public static int HeaderSize { get; } = Marshal.SizeOf(typeof(PacketHeader));
        
        public static int SizeSize { get; } = sizeof(short);
        
        public static int CommandSize { get; } = sizeof(short);


        public short Size { get; }

        public short Command { get; }


        public PacketHeader(short size, short command)
        {
            Size = size;
            Command = command;
        }
    }


    public class SendPacket
    {
        public byte[] Buffer => _buffer;
        private byte[] _buffer;

        public int PacketSize => _packetSize;
        private int _packetSize;


        public SendPacket(byte[] buffer, int packetSize)
        {
            _buffer = buffer;

            _packetSize = packetSize;
        }
    }
}
