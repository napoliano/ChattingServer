using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace Server
{
    public class ChatRoomGroupManager : Singleton<ChatRoomGroupManager>
    {
        private FrozenDictionary<int, ChatRoomGroup> _chatRoomGroups;

        private static int UniqueRoomId;


        public void Initialize()
        {
            var chatRoomGroups = new Dictionary<int, ChatRoomGroup>();

            for (int i = 0; i < GlobalConstants.ChatRoom.MaxChatRoomGroupCount; i++)
            {
                chatRoomGroups[i] = new(i);
            }

            _chatRoomGroups = chatRoomGroups.ToFrozenDictionary();
        }

        public ChatRoomGroup GetChatRoomGroup(int roomId)
        {
            return _chatRoomGroups[roomId % GlobalConstants.ChatRoom.MaxChatRoomGroupCount];
        }

        public int GetUniqueRoomId()
        {
            return Interlocked.Increment(ref UniqueRoomId);
        }
    }
}
