using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public static class PacketHandler
    {
        private static void OnCreateChatRoomRequest(ClientSession session, object message)
        {
            if (message is CsCreateChatRoomRequest request)
            {
                session.User.CreateChatRoom(request.title);
            }
        }

        private static void OnJoinChatRoomRequest(ClientSession session, object message)
        {
            if (message is CsJoinChatRoomRequest request)
            {
                session.User.JoinChatRoom(request.roomId);
            }
        }

        private static void OnLeaveChatRoomRequest(ClientSession session, object message)
        {
            if (message is CsLeaveChatRoomRequest _)
            {
                session.User.LeaveChatRoom();
            }
        }
    }
}
