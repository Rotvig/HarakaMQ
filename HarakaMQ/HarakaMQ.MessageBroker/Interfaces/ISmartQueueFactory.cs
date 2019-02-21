using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface ISmartQueueFactory
    {
        List<ISmartQueue> InitializeSmartQueues(EventHandler<List<Subscriber>> smartQueueOnSubscribersHasBeenUpdated);
        ISmartQueue CreateSmartQueue(EventHandler<List<Subscriber>> smartQueueOnSubscribersHasBeenUpdated, MessageReceivedEventArgs message);
    }
}