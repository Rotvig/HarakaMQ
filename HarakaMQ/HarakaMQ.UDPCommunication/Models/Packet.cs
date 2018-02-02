using System;
using System.Collections.Generic;
using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]
    public class Packet
    {
        public Packet()
        {
        }

        public Packet(int returnPort)
        {
            ReturnPort = returnPort;
            Id = Guid.NewGuid();
        }

        public Packet(int returnPort, List<byte[]> messages)
        {
            Id = Guid.NewGuid();
            ReturnPort = returnPort;
            Messages = messages;
        }

        [Key(0)]
        public List<byte[]> Messages { get; set; }

        [Key(1)]
        public AdministrationMessage AdministrationMessage { get; set; }

        [Key(2)]
        public int SeqNo { get; set; }

        [Key(3)]
        public int ReturnPort { get; set; }

        [Key(4)]
        public Guid Id { get; set; }

        [Key(5)]
        public PacketType Type { get; set; }

        [Key(6)]
        public int? GlobalSequenceNumber { get; set; }

        [Key(7)]
        public TimeSpan? ReceivedAtBroker { get; set; }

        [Key(8)]
        public int? AntiEntropyRound { get; set; }

        [Key(9)]
        public string Topic { get; set; }
    }
}