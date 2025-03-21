using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using UserPacket;
using Google.Protobuf;


namespace ChattingServer
{
    public partial class UserSocketActor : ReceiveActor
    {
        private static void OnCreateChatRoomRequest(UserSocketActor userSocket, IMessage message)
        {
            if (message is CsCreateChatRoomRequest protobufMessage)
            {
                userSocket.User.Tell(new CreateChatRoomRequest(protobufMessage.Title, protobufMessage.UserLimit));
            }
        }

        private static void OnJoinChatRoomRequest(UserSocketActor userSocket, IMessage message)
        {
            if (message is CsJoinChatRoomRequest protobufMessage)
            {
                userSocket.User.Tell(new JoinChatRoomRequest(protobufMessage.RoomId));
            }
        }

        private static void OnLeaveChatRoomRequest(UserSocketActor userSocket, IMessage message)
        {
            if (message is CsLeaveChatRoomRequest protobufMessage)
            {
                userSocket.User.Tell(new LeaveChatRoomRequest());
            }
        }

        private static void OnSendChatMessageRequest(UserSocketActor userSocket, IMessage message)
        {
            if (message is CsSendChatMessageRequest protobufMessage)
            {
                userSocket.User.Tell(new SendChatMessageReqeust(new ChatMessage()
                {
                    Message = protobufMessage.Message,
                    Tick = DateTime.Now.Ticks
                }));
            }
        }
    }
}
