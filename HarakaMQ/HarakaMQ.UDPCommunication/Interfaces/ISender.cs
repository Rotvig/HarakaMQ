using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface ISender
    {
        Task SendMsg(SenderMessage msg, string ip, int port);
    }
}