using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.UDPCommunication
{
    public interface IDynamicRouter : IMessageEvents
    {
        void Send(Message msg, string topic, Broker broker = null);
        void SendAdministrationMessage(AdministrationMessage msg, Broker broker = null);
        Task SendPacket(Packet packet, Broker broker = null);
        void SetupConnection(HarakaMQUDPConfiguration harakaMqudpCopnfiguration);
    }
}