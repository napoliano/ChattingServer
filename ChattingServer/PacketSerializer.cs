using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Google.Protobuf;
using UserPacket;


namespace ChattingServer
{
    public class PacketSerializer
    {
        public static (byte[], int) Serialize<T>(PacketCommand command, T message) where T : IMessage<T>
        {
            int packetSize = PacketHeader.HeaderSize + message.CalculateSize();

            var buffer = new byte[packetSize];
            Span<byte> span = buffer;

            var header = new PacketHeader((short)packetSize, (short)command);
            MemoryMarshal.Write(span, in header);

            message.WriteTo(span.Slice(PacketHeader.HeaderSize));

            return (buffer, packetSize);
        }
    }
}
