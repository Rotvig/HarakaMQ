using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.NET461.Interfaces
{
    public interface IGossip : IAntitropyMethods
    {
        void StartGossip();
        void StopGossip();
        void AntiEntropyMessageReceived(MessageReceivedEventArgs messageReceivedEventArgs);
    }
}