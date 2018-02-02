namespace HarakaMQ.UDPCommunication.Models
{
    public enum UdpMessageType
    {
        Packet,
        ResendRequest,
        GarbageCollect,
        DelayedAck,
        DelayedAckResponse
    }
}