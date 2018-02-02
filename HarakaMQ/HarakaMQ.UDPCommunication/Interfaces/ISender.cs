using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface ISender
    {
        void SendMsg(SenderMessage msg, string ip, int port);
    }
}