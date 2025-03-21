using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Frozen;
using Google.Protobuf;
using UserPacket;


namespace ChattingServer
{
    public static class ProtobufMessageParserManager
    {
        private static FrozenDictionary<PacketCommand, MessageParser> _parsers;


        public static void Initialize()
        {
            var parsers = new Dictionary<PacketCommand, MessageParser>();

            var prefix = "Cs";
            var messageTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.Name.StartsWith(prefix) && typeof(IMessage).IsAssignableFrom(t));

            foreach (var messageType in messageTypes)
            {
                string subMessageTypeName = messageType.Name.Substring(prefix.Length);
                if (Enum.TryParse(subMessageTypeName, out PacketCommand command) == false)
                    continue;

                var parserProperty = messageType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
                var parserPropertyValue = parserProperty?.GetValue(null);
                if (parserPropertyValue is MessageParser parser)
                {
                    parsers[command] = parser;
                }
            }

            _parsers = parsers.ToFrozenDictionary();
        }

        public static IMessage? TryParse(PacketCommand command, byte[] buffer, int offset, int length)
        {
            if (_parsers.TryGetValue(command, out var parser) == false)
                return null;

            return parser.ParseFrom(buffer, offset, length);
        }
    }
}