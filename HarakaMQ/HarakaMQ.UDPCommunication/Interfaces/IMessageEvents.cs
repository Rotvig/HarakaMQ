using System;
using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IMessageEvents
    {
        event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        event EventHandler<MessageReceivedEventArgs> Subscribe;
        event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;
    }
}