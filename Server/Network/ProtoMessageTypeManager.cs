using System;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Frozen;


namespace Server
{
    public class ProtoMessageTypeManager : Singleton<ProtoMessageTypeManager>
    {
        private FrozenDictionary<PacketCommand, Type> _types;
        

        public void Initialize()
        {
            var textInfo = new CultureInfo("en-US", false).TextInfo;

            var requestCommands = new Dictionary<string, PacketCommand>();
            foreach (var command in EnumCache<PacketCommand>.Values)
            {
                var commandString = command.ToString();
                if (commandString.EndsWith("REQUEST") == false)
                    continue;

                var formattedCommand = textInfo.ToTitleCase(commandString.ToLower()).Replace("_", "");
                requestCommands[formattedCommand] = command;
            }

            var temp = new Dictionary<PacketCommand, Type>();

            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Name.StartsWith(GlobalConstants.ProtoMessagePrefix));
            foreach (var type in types)
            {
                if (requestCommands.TryGetValue(type.Name, out var command) == false)
                    continue;

                temp[command] = type;
            }

            _types = temp.ToFrozenDictionary();
        }

        public Type? TryGetType(PacketCommand command)
        {
            _types.TryGetValue(command, out var type);
            return type;
        }
    }
}
