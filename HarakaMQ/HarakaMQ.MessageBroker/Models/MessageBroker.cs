using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.MessageBroker.Models
{
    public class MessageBroker
    {
        public string Name { get; set; }
        public int PrimaryNumber { get; set; }
        
        public Host Host { get; set; }
    }
}