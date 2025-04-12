using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public static class PacketHandler
    {
        private static async Task OnCreateChatRoomRequest(ClientSession session, object message)
        {
            if (message is CsCreateChatRoomRequest request)
            {
                await session.User.CreateChatRoomAysnc(request.title);
            }
        }

        private static async Task OnJoinChatRoomRequest(ClientSession session, object message)
        {
            if (message is CsJoinChatRoomRequest request)
            {
                await session.User.JoinChatRoomAysnc(request.roomId);
            }
        }

        private static async Task OnLeaveChatRoomRequest(ClientSession session, object message)
        {
            if (message is CsLeaveChatRoomRequest request)
            {
                await session.User.LeaveChatRoomAysnc();
            }
        }
    }
}
