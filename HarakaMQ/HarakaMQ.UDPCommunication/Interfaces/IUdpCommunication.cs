using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IUdpCommunication : IMessageEvents
    {
        void Send(Message msg, string topic);
        void Send(Message msg, string ip, int port, string topic);
        void SendAdministrationMessage(AdministrationMessage msg);
        void SendAdministrationMessage(AdministrationMessage msg, string ip, int port);
        void SendPackage(Packet packet, string ip, int port);
        void Listen(int port);
        void SetBrokerInformation(string ip, int port);
        void SetUpUdpComponent(int ackAfterNumOfMessages, int delayedAckWaitTime, bool dontFragment = false, params string[] noDelayedAckClientIds);
    }
}