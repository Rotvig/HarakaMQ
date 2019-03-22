using System;
using System.Net;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IAutomaticRepeatReQuest : IMessageEvents
    {
        void Listen(int port);
        Task Send(Message msg, string topic, Host host);
        void SendAdministrationMessage(AdministrationMessage msg, Host host);
        Task SendPacket(Packet packet, Host host);
    }
}