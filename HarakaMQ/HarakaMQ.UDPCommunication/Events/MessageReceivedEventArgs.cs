using System;
using HarakaMQ.UDPCommunication.Models;
using MessagePack;

namespace HarakaMQ.UDPCommunication.Events
{
    [MessagePackObject]
    public class MessageReceivedEventArgs : EventArgs
    {
        [Key(1)] public AdministrationMessage AdministrationMessage;

        [Key(0)] public string IpAddress;

        [Key(2)] public int Port;

        [Key(3)] public UdpMessageType Type;
        
        [IgnoreMember]
        public string SenderClient => IpAddress + Port;
    }
}