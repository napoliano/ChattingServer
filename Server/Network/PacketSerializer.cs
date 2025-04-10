using System;
using System.IO;
using ProtoBuf;


namespace Server
{
    public static class PacketSerializer
    {
        public static SendPacket MakeSendPacket<T>(PacketCommand command, T message) where T : class, IExtensible
        {
            var packet = ObjectPool<SendPacket>.Rent();

            using var ms = new MemoryStream(packet.Buffer);
            using var writer = new BinaryWriter(ms);

            var measure = Serializer.Measure(message);
            short packetSize = (short)(PacketHeader.HeaderSize + measure.Length);

            packet.SetPacketSize(packetSize);

            writer.Write(packetSize);
            writer.Write((short)command);

            Serializer.Serialize(writer.BaseStream, message);

            return packet;
        }
    }
}
