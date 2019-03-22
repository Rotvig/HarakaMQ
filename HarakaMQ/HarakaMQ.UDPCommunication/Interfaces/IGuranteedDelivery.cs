using System;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IGuranteedDelivery
    {
        void Listen(int port);
        Task Send(ExtendedPacketInformation msg);
        void ReSend(Guid messageId);
        void ReSend(string clientId, int seqNo);
        void SendUdpMessage(UdpMessage msg, UdpMessageType type, Host host);
        Task RemoveMessagesFromSendQueueAsync(string clientId, int seqNo);
        Task RemoveMessageFromReceiveQueueAsync(Guid msgid);
        event EventHandler<ExtendedPacketInformation> MessageReceived;
    }
}