using System.Net;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IDynamicRouter : IMessageEvents
    {
        void Send(Message msg, string topic, Host host = null);
        void SendAdministrationMessage(AdministrationMessage msg, Host host = null);
        Task SendPacket(Packet packet, Host host = null);
        void SetupConnection(IHarakaMQUDPConfiguration harakaMqudpConfiguration);
    }
}