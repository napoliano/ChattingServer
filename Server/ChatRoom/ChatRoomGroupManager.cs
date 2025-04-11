using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Server
{
    public class ChatRoomGroupManager : Singleton<ChatRoomGroupManager>
    {
        private static int s_newRoomId;

        private FrozenDictionary<int, ChatRoomGroup> _chatRoomGroups;
        private int _groupCount;


        public void Initialize(int groupCount)
        {
            var chatRoomGroups = new Dictionary<int, ChatRoomGroup>();

            for (int i = 0; i < groupCount; i++)
            {
                chatRoomGroups[i] = new();
            }

            _chatRoomGroups = _chatRoomGroups.ToFrozenDictionary();
            _groupCount = groupCount;
        }

        public ChatRoomGroup GetChatRoomGroup(int roomId)
        {
            return _chatRoomGroups[roomId % _groupCount];
        }

        public int GetNewRoomId()
        {
            int newRoomId = Interlocked.Increment(ref s_newRoomId);
            return newRoomId;
        }
    }
}
