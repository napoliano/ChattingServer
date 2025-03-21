using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using UserPacket;


namespace ChattingServer
{
    public class ChatRoomActor : ReceiveActor
    {
        private static IActorRef _chatRoomManager;

        private readonly Dictionary<int, IActorRef> _receivers = new();
        private readonly Dictionary<int, string> _receiverNames = new();

        private readonly int _roomId;

        private readonly string _title;

        private int _userCount;
        private readonly int _userLimit;

        private bool _destroy;


        public static Props Props(int roomId, string title, int userLimit) => Akka.Actor.Props.Create(() => new ChatRoomActor(roomId, title, userLimit));

        public ChatRoomActor(int roomId, string title, int userLimit)
        {
            _roomId = roomId;
            _title = title;
            _userLimit = userLimit;


            //채팅 방 입장
            Receive<JoinChatRoom>(message =>
            {
                if (_destroy)
                {
                    Sender.Tell(new JoinChatRoomFailure(ServerErrrorCode.ChatRoomDestroyed));
                    return;
                }

                if (_userLimit <= _userCount)
                {
                    Sender.Tell(new JoinChatRoomFailure(ServerErrrorCode.ChatRoomCapacityExceeded));
                    return;
                }

                if (_receivers.TryAdd(message.UserId, Sender) == false)
                {
                    Sender.Tell(new JoinChatRoomFailure(ServerErrrorCode.ChatRoomAlreadyJoined));
                    return;
                }

                _receiverNames[message.UserId] = message.UserName;

                ++_userCount;

                var chatRoomInfo = MakeChatRoomInfo();
                Sender.Tell(new JoinChatRoomSuccess(chatRoomInfo, message.AutoJoin));
            });

            //채팅 방 퇴장
            Receive<LeaveChatRoom>(message =>
            {
                if (_receivers.TryGetValue(message.UserId, out var target) == false)
                {
                    Sender.Tell(new LeaveChatRoomFailure(ServerErrrorCode.UserNotFoundInChatRoom));
                    return;
                }

                _receivers.Remove(message.UserId);
                _receiverNames.Remove(message.UserId);

                --_userCount;

                Sender.Tell(new LeaveChatRoomSuccess());

                //모든 유저가 퇴장한 경우 채팅 방 제거 요청
                if (_userCount == 0)
                {
                    _destroy = true;
                    _chatRoomManager.Tell(new DestroyChatRoom(_roomId));
                }
            });

            //모든 수신자에게 메시지 전달
            Receive<BroadcastChatMessage>(message =>
            {
                if (_destroy)
                    return;

                foreach (var receiver in _receivers.Values)
                {
                    receiver.Tell(new SendChatMessageToUser(message.ChatMessage));
                }
            });
        }

        private ChatRoomInfo MakeChatRoomInfo()
        {
            var chatRoomInfo = new ChatRoomInfo();

            chatRoomInfo.RoomId = _roomId;
            chatRoomInfo.Title = _title;

            foreach (var receiverName in _receiverNames.Values)
            {
                chatRoomInfo.ReceiverNames.Add(receiverName);
            }

            return chatRoomInfo;
        }

        public static void SetChatRoomManager(IActorRef chatRoomManager)
        {
            _chatRoomManager = chatRoomManager;
        }
    }
}
