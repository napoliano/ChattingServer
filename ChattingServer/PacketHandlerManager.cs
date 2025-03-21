using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Frozen;
using Google.Protobuf;
using UserPacket;


namespace ChattingServer
{
    public static class PacketHandlerManager
    {
        private static FrozenDictionary<PacketCommand, Action<UserSocketActor, IMessage>> _handlers;


        public static void Initialize()
        {
            var handlers = new Dictionary<PacketCommand, Action<UserSocketActor, IMessage>>();

            var prefix = "On";
            var methods = typeof(UserSocketActor)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name.StartsWith(prefix));

            foreach (var method in methods)
            {
                string subMethodName = method.Name.Substring(prefix.Length);
                if (Enum.TryParse(subMethodName, out PacketCommand command) == false)
                    continue;

                handlers[command] = (Action<UserSocketActor, IMessage>)Delegate.CreateDelegate(typeof(Action<UserSocketActor, IMessage>), method);
            }

            _handlers = handlers.ToFrozenDictionary();
        }

        public static bool TryHandle(PacketCommand command, UserSocketActor userSocket, IMessage message)
        {
            if (_handlers.TryGetValue(command, out var handler) == false)
                return false;

            handler.Invoke(userSocket, message);
            return true;
        }
    }
}