using System;
using System.Collections.Generic;
using HarakaMQ.UDPCommunication.Models;
using MessagePack;

namespace HarakaMQ.MessageBroker.Models
{
    [MessagePackObject]
    public class Subscriber
    {
        public Subscriber(Host host, int attachedBroker, string clientId)
        {
            Host = host;
            ClientId = clientId;
            MessagesReceived = new List<Guid>();
            AttachedBroker = attachedBroker;
        }

        public Subscriber()
        {
            MessagesReceived = new List<Guid>();
        }

        [Key(0)]
        public Host Host { get; set; }
            
        [Key(1)]
        public string ClientId { get; set; }

        [Key(2)]
        public int GlobalSequenceNumberLastReceived { get; set; }

        [Key(3)]
        public int AttachedBroker { get; set; }

        [Key(4)]
        public bool ReceiveTentativeMessages { get; set; }

        /// <summary>
        ///     MessagesReceived is purged after 2 rounds of Anti-entropy
        ///     MessagesReceived only contains tentative messages
        /// </summary>
        [Key(6)]
        public List<Guid> MessagesReceived { get; set; }

        public void UpdateGobalSequenceNumber(int number)
        {
            GlobalSequenceNumberLastReceived = number;
        }
    }
}