using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using UserPacket;


namespace ChattingServer
{
    public class ChatRoomManagerActor : ReceiveActor
    {
        private readonly Dictionary<int, IActorRef> _rooms = new();

        private int _lastRoomId = 0;


        public static readonly Props Props = Props.Create(() => new ChatRoomManagerActor());

        public ChatRoomManagerActor()
        {
            Receive<CreateChatRoom>(message =>
            {
                if (IsValiTitle(message.Title) == false)
                {
                    Sender.Tell(new CreateChatRoomFailure(ServerErrrorCode.ChatRoomInvalidTitle));
                    return;
                }
                
                if (IsValidUserLimit(message.UserLimit) == false)
                {
                    Sender.Tell(new CreateChatRoomFailure(ServerErrrorCode.ChatRoomInvalidMaxUsers));
                    return;
                }

                int roomId = _lastRoomId + 1;

                var room = Context.ActorOf(ChatRoomActor.Props(roomId, message.Title, message.UserLimit), $"{nameof(ChatRoomActor)}_{roomId}");
                _rooms[roomId] = room;

                _lastRoomId = roomId;

                Sender.Tell(new CreateChatRoomSuccess(roomId, room));
            });


            Receive<DestroyChatRoom>(message =>
            {
                if (_rooms.TryGetValue(message.RoomId, out var room) == false)
                    return;

                _rooms.Remove(message.RoomId);

                Context.Stop(room);
            });


            Receive<FindChatRoom>(message =>
            {
                if (_rooms.TryGetValue(message.RoomId, out var room) == false)
                {
                    Sender.Tell(new Status.Failure(new Exception($"Not found room - roomId:{message.RoomId}")));
                    return;
                }

                Sender.Tell(room);
            });
        }

        private bool IsValiTitle(string title)
        {
            return true;
        }

        private bool IsValidUserLimit(int userLimit)
        {
            return true;
        }
    }
}
