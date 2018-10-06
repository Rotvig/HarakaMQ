using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker.NET461.Events;
using HarakaMQ.MessageBroker.NET461.Models;

namespace HarakaMQ.MessageBroker.NET461.Interfaces
{
    public interface IPersistenceLayer
    {
        event EventHandler EventAdded;
        void AddEvent(Topic topic, Event @event);
        Event GetNextEvent(Topic topic);
        void EventHasBeenhandled(Topic topic, Guid eventId);
        void AddSubscriber(Topic topic, Subscriber subscriber);
        void UpdateSubscribers(Topic topic);
        void UpdatelastGlobalSeqReceived(Topic topic, int seqNo);
        List<Subscriber> GetSubScribers(Topic topic);
        void AddSubscribers(Topic topic, List<Subscriber> eventSubscribers);
    }
}