using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace Server
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PacketHeader
    {
        public static int HeaderSize { get; } = Marshal.SizeOf(typeof(PacketHeader));

        public short Size { get; }
        
        public short Command { get; }

        public PacketHeader(short size, short command)
        {
            Size = size;
            Command = command;
        }
    }


    public abstract class PacketBase
    {
        public static readonly int MaxPacketSize = 8192;

        public int PacketSize => _packetSize;
        protected int _packetSize;


        public void SetPacketSize(int packetSize)
        {
            _packetSize = packetSize;
        }
    }


    public class SendPacket : PacketBase, IResettable
    {
        public byte[] Buffer => _buffer;
        private readonly byte[] _buffer = new byte[GlobalConstants.Network.MaxPacketSize];


        public void Reset()
        {
            Array.Clear(_buffer, 0, GlobalConstants.Network.MaxPacketSize);

            _packetSize = 0;
        }
    }
}
