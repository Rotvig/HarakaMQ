using System;
using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]
    public class UdpMessage
    {
        [Key(0)]
        public int SeqNo { get; set; }

        [Key(1)]
        public int ReturnPort { get; set; }

        [Key(2)]
        public Guid? MessageId { get; set; }

        [Key(3)]
        public bool InOrder { get; set; }
    }
}