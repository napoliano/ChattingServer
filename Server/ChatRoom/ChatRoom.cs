using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public class ChatRoom
    {
        public string Title => _title;
        private readonly string _title;

        private readonly Dictionary<int, IParticipant> _participants = new();


        public ChatRoom(string title)
        {
            _title = title;
        }

        /// <summary>
        /// 채팅 방 입장
        /// </summary>
        public (bool success, ServerErrorCode errorCode) Join(IParticipant participant)
        {
            //이미 참여한 채팅 방인 경우
            if (_participants.ContainsKey(participant.Id))
                return (false, ServerErrorCode.ChatRoomJoinFailed);

            //입장 시스템 메시지
            BroadcastSystemMessage($"{participant.Name} participated in the chat room");

            _participants[participant.Id] = participant;

            return (false, ServerErrorCode.None);
        }

        /// <summary>
        /// 채팅 방 퇴장
        /// </summary>
        public void Leave(IParticipant participant)
        {
            if (_participants.Remove(participant.Id))
            {
                //퇴장 메시지
                BroadcastSystemMessage($"{participant.Name} left the chat room");
            }
        }

        /// <summary>
        /// 모든 참가자 대상으로 메시지 전달
        /// </summary>
        public void Broadcast(ChatMessage chatMessage)
        {
            foreach (var participant in _participants.Values)
            {
                participant.ReceiveMessage(chatMessage);
            }
        }

        /// <summary>
        /// 모든 참가자 대상으로 시스템 메시지 전달
        /// </summary>
        private void BroadcastSystemMessage(string message)
        {
            Broadcast(new ChatMessage()
            {
                senderName = GlobalConstants.ChatRoom.SystemName,
                message = message
            });
        }

        /// <summary>
        /// 유저 수 반환
        /// </summary>
        public int GetUserCount()
        {
            return _participants.Values.Count(p => p.IsUser);
        }

        /// <summary>
        /// 채팅 방에 유저가 비어있는지 여부 반환
        /// </summary>
        public bool IsEmpty()
        {
            return GetUserCount() == 0;
        }

        /// <summary>
        /// 모든 유저들의 이름 반환
        /// </summary>
        public List<string> GetUserNameList()
        {
            var names = new List<string>();

            foreach (var participants in _participants.Values)
            {
                if (participants.IsUser)
                    names.Add(participants.Name);
            }

            return names;
        }
    }
}
