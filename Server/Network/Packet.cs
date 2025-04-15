using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace Server
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PacketHeader(short size, short command)
    {
        public static int HeaderSize { get; } = Marshal.SizeOf(typeof(PacketHeader));

        public short Size { get; } = size;

        public short Command { get; } = command;
    }


    public abstract class PacketBase
    {
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
