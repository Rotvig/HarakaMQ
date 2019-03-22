using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface ISender
    {
        Task SendMsg(SenderMessage msg, Host host);
        Task SendMsg(SenderMessage msg, string iPAddress, int port);

    }
}