using System;
using Akka.Actor;


namespace ChattingServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            PacketHandlerManager.Initialize();
            ProtobufMessageParserManager.Initialize();
            
            using ActorSystem actorSystem = ActorSystem.Create("ActorSystem");

            var chatServer = actorSystem.ActorOf(ChatServerActor.Props, nameof(ChatServerActor));
            
            //[Todo] 포트 번호 config로 뺄 것
            chatServer.Tell(new StartServer(7001));

            while (true)
            {
                var command = Console.ReadLine();
                if (command?.ToLower().Equals("exit") ?? false)
                {
                    actorSystem.Stop(chatServer);
                    break;
                }
            }
        }
    }
}
