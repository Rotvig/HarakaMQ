using System;
using HarakaMQ.UDPCommunication.Models;
using MessagePack;

namespace HarakaMQ.UDPCommunication.Events
{
    [MessagePackObject]
    public class PublishPacketReceivedEventArgs : EventArgs
    {
        [Key(0)] public string IpAddress;

        [Key(1)] public Packet Packet;

        [Key(2)] public int Port;

        [IgnoreMember]
        public string SenderClient => IpAddress + Packet.ReturnPort;
    }
}