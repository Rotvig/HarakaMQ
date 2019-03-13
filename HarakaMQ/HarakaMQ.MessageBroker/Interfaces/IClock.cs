using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface IClock
    {
        void StartTimeSync(List<MessageBrokerInformation> brokersToSync);
        TimeSpan ElapsedTimeSpan();
        void ClockSyncMessageReceived(MessageReceivedEventArgs receivedMessage);
    }
}