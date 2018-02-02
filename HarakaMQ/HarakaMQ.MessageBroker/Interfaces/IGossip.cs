using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface IGossip : IAntitropyMethods
    {
        void StartGossip();
        void StopGossip();
        void AntiEntropyMessageReceived(MessageReceivedEventArgs messageReceivedEventArgs);
        void ClockSyncMessageReceived(MessageReceivedEventArgs messageReceivedEventArgs);
    }
}