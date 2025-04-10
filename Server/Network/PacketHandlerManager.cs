using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Frozen;


namespace Server
{
    public class PacketHandlerManager : Singleton<PacketHandlerManager>
    {
        private FrozenDictionary<PacketCommand, Action<ClientSession, object>> _handlers;


        public void Initialize()
        {
            var handlers = new Dictionary<PacketCommand, Action<ClientSession, object>>();

            var methods = typeof(PacketHandler).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name.StartsWith(GlobalConstants.Network.PacketHandlerPrefix));
            foreach (var method in methods)
            {
                string subMethodName = method.Name[GlobalConstants.Network.PacketHandlerPrefix.Length..];
                if (Enum.TryParse(subMethodName, out PacketCommand command) == false)
                    continue;

                handlers[command] = (Action<ClientSession, object>)Delegate.CreateDelegate(typeof(Action<ClientSession, object>), method);
            }

            _handlers = handlers.ToFrozenDictionary();
        }

        public void Handle(PacketCommand command, ClientSession userSocket, object message)
        {
            _handlers.TryGetValue(command, out var handle);
            handle?.Invoke(userSocket, message);
        }
    }
}
