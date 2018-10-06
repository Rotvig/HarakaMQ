using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.NET461.Interfaces
{
    public interface IAntitropyMethods
    {
        void SubScribeMessageReceived(MessageReceivedEventArgs message);
        void PublishMessageReceived(PublishPacketReceivedEventArgs publishPacketReceivedEventArgs);
        void QueueDeclareMessageReceived(MessageReceivedEventArgs message);
    }
}