using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.UDPCommunication
{
    public interface IDynamicRouter : IMessageEvents
    {
        void Send(Message msg, string topic);
        void SendAdministrationMessage(AdministrationMessage msg);
        void SendPackage(Packet packet);
        void SetupConnection(HarakaMQUDPConfiguration harakaMqudpConfiguration);
    }
}