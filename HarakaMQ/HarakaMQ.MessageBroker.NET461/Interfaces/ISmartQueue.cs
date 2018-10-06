using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker.NET461.Events;
using HarakaMQ.MessageBroker.NET461.Models;

namespace HarakaMQ.MessageBroker.NET461.Interfaces
{
    internal interface ISmartQueue
    {
        string GetTopicId();
        void AddEvent(Event @event);
        void UpdateLastGlobalSeqNoReceived(int seqNo);
        int GetLastGlobalSeqReceived();


        event EventHandler<List<Subscriber>> SubscribersHasBeenUpdated;
    }
}