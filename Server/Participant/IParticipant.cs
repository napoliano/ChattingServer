using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public interface IParticipant
    {
        int Id { get; }

        string Name { get; }

        bool IsUser { get; }

        void ReceiveMessage(ChatMessage chatMessage);
    }
}
