using MessagePack;

namespace HarakaMQ.MessageBroker.NET461.Models
{
    [MessagePackObject]
    public class Publisher
    {
        public Publisher(string ip, int port, int attachedBroker, string clientId)
        {
            Ip = ip;
            Port = port;
            ClientId = clientId;
            AttachedBroker = attachedBroker;
        }

        public Publisher()
        {
        }

        [Key(0)]
        public string ClientId { get; set; }

        [Key(1)]
        public string Ip { get; set; }

        [Key(2)]
        public int Port { get; set; }

        [Key(3)]
        public int AttachedBroker { get; set; }

        public void SetIpAndPort(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }
    }
}