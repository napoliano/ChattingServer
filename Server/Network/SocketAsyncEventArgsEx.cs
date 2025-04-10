using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;


namespace Server
{
    public class SocketAsyncEventArgsEx : IResettable
    {
        public SocketAsyncEventArgs SAEA => _saea;
        private readonly SocketAsyncEventArgs _saea = new();

        public byte[] Buffer => _buffer;
        private readonly byte[] _buffer = GC.AllocateArray<byte>(GlobalConstants.MaxPacketSize, true);


        public void Reset()
        {
            _saea.SetBuffer(null, 0, 0);

            Array.Clear(_buffer, 0, GlobalConstants.MaxPacketSize);
        }
    }
}
