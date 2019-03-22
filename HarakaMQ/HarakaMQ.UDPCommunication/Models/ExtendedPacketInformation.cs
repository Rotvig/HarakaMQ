using System;
using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]
    public class ExtendedPacketInformation
    {
        public ExtendedPacketInformation()
        {
        }

        public ExtendedPacketInformation(Packet packet, UdpMessageType type, Host host)
        {
            Id = Guid.NewGuid();
            UdpMessageType = type;
            Packet = packet;
            Host = host;
        }
        
        public ExtendedPacketInformation(Packet packet, UdpMessageType type, string ip, int port)
        {
            Id = Guid.NewGuid();
            UdpMessageType = type;
            Packet = packet;
            Host = new Host(){IPAddress = ip, Port = port};
        }

        public ExtendedPacketInformation(Packet packet, UdpMessageType type, string ip)
        {
            Id = Guid.NewGuid();
            UdpMessageType = type;
            Host = new Host(){IPAddress = ip, Port = packet.ReturnPort};
            Packet = packet;
        }

        public ExtendedPacketInformation(UdpMessage msg, UdpMessageType type, string ip)
        {
            Id = Guid.NewGuid();
            UdpMessageType = type;
            UdpMessage = msg;
            Host = new Host(){IPAddress = ip, Port = msg.ReturnPort};
        }

        [Key(0)]
        public Guid Id { get; set; }

        [Key(1)]
        public Packet Packet { get; set; }

        [Key(2)]
        public UdpMessage UdpMessage { get; set; }

        [Key(3)]
        public UdpMessageType UdpMessageType { get; set; }

        //Host is for orignal reciever 
        [Key(4)]
        public Host Host { get; set; }

        [IgnoreMember]
        public string SenderClient => Host.IPAddress + Host.Port;
    }
}