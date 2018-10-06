using System.Collections.Generic;
using HarakaMQ.UDPCommunication.Events;
using MessagePack;

namespace HarakaMQ.MessageBroker.NET461.Models
{
    [MessagePackObject]
    public class AntiEntropyMessage
    {
        [Key(7)] public bool AntiEntropyGarbageCollect = false;

        [Key(5)] public int AntiEntropyRound = 0;

        [Key(6)] public int Primary = 0;

        [Key(0)]
        public int PrimaryNumber { get; set; }

        [Key(1)]
        public List<PublishPacketReceivedEventArgs> Tentative { get; set; }

        [Key(2)]
        public List<PublishPacketReceivedEventArgs> Committed { get; set; }

        [Key(3)]
        public List<Subscriber> Subscribers { get; set; }

        [Key(4)]
        public List<Publisher> Publishers { get; set; }
    }
}