using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Threading.Tasks;


namespace Server
{
    public class PacketHandlerManager : Singleton<PacketHandlerManager>
    {
        private FrozenDictionary<PacketCommand, Func<ClientSession, object, Task>> _handlers;


        public void Initialize()
        {
            var handlers = new Dictionary<PacketCommand, Func<ClientSession, object, Task>>();

            var methods = typeof(PacketHandler).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name.StartsWith(GlobalConstants.Network.PacketHandlerPrefix));
            foreach (var method in methods)
            {
                string subMethodName = GetPacketCommandNameFromMethod(method);
                if (Enum.TryParse(subMethodName, out PacketCommand command) == false)
                    continue;

                handlers[command] = (Func<ClientSession, object, Task>)Delegate.CreateDelegate(typeof(Func<ClientSession, object, Task>), method);
            }

            _handlers = handlers.ToFrozenDictionary();
        }

        public async Task HandleAsync(PacketCommand command, ClientSession userSocket, object message)
        {
            if (_handlers.TryGetValue(command, out var handle))
            {
                await handle(userSocket, message);
            }
            else
            {
                Log.Error($"No handler found - command:{command}");
            }
        }

        private string GetPacketCommandNameFromMethod(MethodInfo method)
        {
            string name = method.Name;

            if (name.StartsWith(GlobalConstants.Network.PacketHandlerPrefix))
                name = name[GlobalConstants.Network.PacketHandlerPrefix.Length..];

            if (name.EndsWith("Async"))
                name = name[..^5];

            return GlobalConstants.Network.ProtoMessagePrefix + name;
        }
    }
}
