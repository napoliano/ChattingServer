using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public record ChatMessage(string SenderName, string Message)
    {
        public DateTime SendTime { get; } = DateTime.Now;
    }
}
