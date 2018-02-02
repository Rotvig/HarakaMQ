using System;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IGuranteedDelivery
    {
        void Listen(int port);
        void Send(ExtendedPacketInformation msg);
        void ReSend(Guid messageId);
        void ReSend(string clientId, int seqNo);
        void SendUdpMessage(UdpMessage msg, UdpMessageType type, string ip, int port);
        Task RemoveMessagesFromSendQueueAsync(string clientId, int seqNo);
        Task RemoveMessageFromReceiveQueueAsync(Guid msgid);
        event EventHandler<ExtendedPacketInformation> MessageReceived;
    }
}