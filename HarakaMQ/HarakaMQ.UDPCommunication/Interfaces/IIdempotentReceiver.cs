using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IIdempotentReceiver
    {
        bool VerifyPacket(Packet packet);
    }
}