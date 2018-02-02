using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]
    public class SenderMessage
    {
        [Key(0)]
        public byte[] Body { get; set; }

        [Key(1)]
        public UdpMessageType Type { get; set; }
    }
}