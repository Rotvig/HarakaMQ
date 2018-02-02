using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.UDPCommunication.Events;
using MessagePack;

namespace HarakaMQ.MessageBroker.Events
{
    [MessagePackObject]
    public class Event
    {
        public Event(dynamic content, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.TentativeMessage:
                    Message = content;
                    break;
                case EventType.AddSubscriber:
                    Subscriber = content;
                    break;
                case EventType.ComittedMessages:
                    Messages = content;
                    break;
                case EventType.TentativeMessages:
                    Messages = content;
                    break;
                case EventType.ComittedMessage:
                    Message = content;
                    break;
                case EventType.AddSubscribers:
                    Subscribers = content;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
            }

            EventType = eventType;
            Id = Guid.NewGuid();
        }

        public Event()
        {
        }

        [Key(0)]
        public Guid Id { get; set; }

        [Key(1)]
        public EventType EventType { get; set; }

        [Key(2)]
        public Subscriber Subscriber { get; set; }

        [Key(3)]
        public List<Subscriber> Subscribers { get; set; }

        [Key(4)]
        public PublishPacketReceivedEventArgs Message { get; set; }

        [Key(5)]
        public List<PublishPacketReceivedEventArgs> Messages { get; set; }
    }

    public enum EventType
    {
        TentativeMessage,
        AddSubscriber,
        ComittedMessages,
        TentativeMessages,
        ComittedMessage,
        AddSubscribers
    }
}