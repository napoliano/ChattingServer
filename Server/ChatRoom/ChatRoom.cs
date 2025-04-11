using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public class ChatRoom
    {
        private readonly string _title;
        private static readonly string s_systemName = "System";

        private readonly Dictionary<int, IParticipant> _participants = new();


        public ChatRoom(string title)
        {
            _title = title;
        }

        public bool Join(IParticipant participant)
        {
            if (_participants.ContainsKey(participant.Id))
                return false;

            //입장 메시지
            Broadcast(new ChatMessage(s_systemName, $"{participant.Name} participated in the chat room"));

            _participants[participant.Id] = participant;

            return true;
        }

        public void Leave(IParticipant participant)
        {
            _participants.Remove(participant.Id);

            //퇴장 메시지
            Broadcast(new ChatMessage(s_systemName, $"{participant.Name} left the chat room"));
        }

        public int GetParticipantCount()
        {
            return _participants.Count;
        }

        public void Broadcast(ChatMessage chatMessage)
        {
            foreach (var participant in _participants.Values)
            {
                participant.ReceiveMessage(chatMessage);
            }
        }
    }
}
