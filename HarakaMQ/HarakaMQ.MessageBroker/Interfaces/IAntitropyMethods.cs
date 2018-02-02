using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface IAntitropyMethods
    {
        void SubScribeMessageReceived(MessageReceivedEventArgs message);
        void PublishMessageReceived(PublishPacketReceivedEventArgs publishPacketReceivedEventArgs);
        void QueueDeclareMessageReceived(MessageReceivedEventArgs message);
    }
}