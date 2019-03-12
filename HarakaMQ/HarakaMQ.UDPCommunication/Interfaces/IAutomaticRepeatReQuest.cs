using System;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IAutomaticRepeatReQuest : IMessageEvents
    {
        void Listen(int port);
        Task Send(Message msg, string topic, Broker broker);
        void SendAdministrationMessage(AdministrationMessage msg, Broker broker);
        Task SendPacket(Packet packet, Broker broker);
    }
}