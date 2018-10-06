using System;
using System.Collections.Generic;
using MessagePack;

namespace HarakaMQ.MessageBroker.NET461.Models
{
    [MessagePackObject]
    public class Subscriber
    {
        public Subscriber(string ip, int port, int attachedBroker, string clientId)
        {
            Ip = ip;
            Port = port;
            ClientId = clientId;
            MessagesReceived = new List<Guid>();
            AttachedBroker = attachedBroker;
        }

        public Subscriber()
        {
            MessagesReceived = new List<Guid>();
        }

        [Key(0)]
        public string Ip { get; set; }

        [Key(1)]
        public int Port { get; set; }

        [Key(2)]
        public string ClientId { get; set; }

        [Key(3)]
        public int GlobalSequenceNumberLastReceived { get; set; }

        [Key(4)]
        public int AttachedBroker { get; set; }

        [Key(5)]
        public bool ReceiveTentativeMessages { get; set; }

        /// <summary>
        ///     MessagesReceived is purged after 2 rounds of Anti-entropy
        ///     MessagesReceived only contains tentative messages
        /// </summary>
        [Key(6)]
        public List<Guid> MessagesReceived { get; set; }

        public void SetIpAndPort(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        public void UpdateGobalSequenceNumber(int number)
        {
            GlobalSequenceNumberLastReceived = number;
        }
    }
}