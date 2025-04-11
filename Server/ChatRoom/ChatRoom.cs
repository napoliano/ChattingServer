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

        private readonly Dictionary<int, IParticipant> _participants = new();

        private static readonly string _systemName = "System";


        public ChatRoom(string title)
        {
            _title = title;
        }

        public bool Join(IParticipant participant)
        {
            if (_participants.ContainsKey(participant.Id))
                return false;

            _participants[participant.Id] = participant;

            //입장 메시지
            Broadcast(new ChatMessage(_systemName, $"{participant.Name} participated in the chat room"));

            return true;
        }

        public void Leave(IParticipant participant)
        {
            _participants.Remove(participant.Id);

            //퇴장 메시지
            Broadcast(new ChatMessage(_systemName, $"{participant.Name} left the chat room"));
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
