namespace HarakaMQ.UDPCommunication.Models
{
    public enum MessageType
    {
        QueueDeclare,
        Subscribe,
        Unsubscribe,
        Publish,
        AntiEntropy,
        ClockSync
    }
}