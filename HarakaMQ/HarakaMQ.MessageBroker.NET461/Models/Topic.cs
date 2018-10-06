using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker.NET461.Events;
using MessagePack;

namespace HarakaMQ.MessageBroker.NET461.Models
{
    [MessagePackObject]
    public class Topic
    {
        public Topic(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Subscribers = new List<Subscriber>();
            Events = new List<Event>();
        }

        public Topic()
        {
            Id = Guid.NewGuid();
            Subscribers = new List<Subscriber>();
            Events = new List<Event>();
        }

        [Key(0)]
        public Guid Id { get; set; }

        [Key(1)]
        public List<Subscriber> Subscribers { get; set; }

        [Key(2)]
        public int LastCommittedGlobalSeqNumberReceived { get; set; }

        /// <summary>
        ///     Contains unhandled committed/tentative messages
        ///     Containt unhandled subscribers
        /// </summary>
        [Key(3)]
        public List<Event> Events { get; set; }

        [Key(4)]
        public string Name { get; set; }
    }
}