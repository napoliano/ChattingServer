using System.Collections.Generic;
using Akka.Actor;


namespace ChattingServer
{
    public class UserSocketManagerActor : ReceiveActor
    {
        private readonly Dictionary<int, IActorRef> _userSockets = new();
        private int _lastUserSocketId = 0;


        public static readonly Props Props = Props.Create(() => new UserSocketManagerActor());

        public UserSocketManagerActor()
        {
            Receive<AddUserSocket>(message =>
            {
                int userSocketId = _lastUserSocketId + 1;

                var userSocket = Context.ActorOf(UserSocketActor.Props(userSocketId, message.Socket), $"{nameof(UserSocketActor)}_{userSocketId}");
                _userSockets[userSocketId] = userSocket;

                _lastUserSocketId = userSocketId;

                userSocket.Tell(new StartReceive());
            });

            Receive<RemoveUserSocket>(message =>
            {
                if (false == _userSockets.TryGetValue(message.Id, out var userSocekt))
                    return;

                _userSockets.Remove(message.Id);

                Context.Stop(userSocekt);
            });
        }
    }
}
