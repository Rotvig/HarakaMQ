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

        public ExtendedPacketInformation(Guid messageId)
        {
            Id = messageId;
        }

        public ExtendedPacketInformation(Packet packet, UdpMessageType type, string ip, int port)
        {
            Id = Guid.NewGuid();
            UdpMessageType = type;
            Ip = ip;
            Port = port;
            Packet = packet;
        }

        public ExtendedPacketInformation(Packet packet, UdpMessageType type, string ip)
        {
            Id = Guid.NewGuid();
            UdpMessageType = type;
            Ip = ip;
            Port = packet.ReturnPort;
            Packet = packet;
        }

        public ExtendedPacketInformation(UdpMessage msg, UdpMessageType type, string ip)
        {
            Id = Guid.NewGuid();
            UdpMessageType = type;
            UdpMessage = msg;
            Ip = ip;
            Port = msg.ReturnPort;
        }

        [Key(0)]
        public Guid Id { get; set; }

        [Key(1)]
        public Packet Packet { get; set; }

        [Key(2)]
        public UdpMessage UdpMessage { get; set; }

        [Key(3)]
        public UdpMessageType UdpMessageType { get; set; }

        //Ip and port is for orignal reciever 
        [Key(4)]
        public string Ip { get; set; }

        [Key(5)]
        public int Port { get; set; }

        [IgnoreMember]
        public string SenderClient => Ip + Port;

        public void SetUdpMessageType(UdpMessageType type)
        {
            UdpMessageType = type;
        }

        public void SetIpAndPort(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        public void SetId(Guid id)
        {
            Id = id;
        }
    }
}