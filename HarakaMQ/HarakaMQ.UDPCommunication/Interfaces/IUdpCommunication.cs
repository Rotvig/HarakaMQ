using System.Net;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IUdpCommunication : IMessageEvents
    {
        void Send(Message msg, string topic, Host host = null);
        void SendAdministrationMessage(AdministrationMessage msg, Host host = null);
        void SendPackage(Packet packet, Host host = null);
        void Listen(IHarakaMQUDPConfiguration harakaMqudpConfiguration);
    }
}