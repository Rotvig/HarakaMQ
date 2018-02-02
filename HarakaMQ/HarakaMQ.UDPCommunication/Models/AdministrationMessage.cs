using System;
using HarakaMQ.UDPCommunication.Utils;
using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]
    public class AdministrationMessage
    {
        public AdministrationMessage()
        {
        }

        public AdministrationMessage(MessageType type, string topic)
        {
            Type = type;
            Topic = topic;
        }

        public AdministrationMessage(MessageType type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        [Key(0)]
        public string Topic { get; set; }
        [Key(1)]
        public MessageType Type { get; set; }
        [Key(2)]
        public byte[] Data { get; set; }
    }
}