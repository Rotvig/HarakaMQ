using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker.Events;
using HarakaMQ.MessageBroker.Models;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface ISmartQueue
    {
        string GetTopicId();
        void AddEvent(Event @event);
        void UpdateLastGlobalSeqNoReceived(int seqNo);
        int GetLastGlobalSeqReceived();

        event EventHandler<List<Subscriber>> SubscribersHasBeenUpdated;
    }
}