using System;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IAutomaticRepeatReQuest : IMessageEvents
    {
        void Listen(int port);
        Task Send(Message msg, string ip, int port, string topic);
        void SendAdministrationMessage(AdministrationMessage msg, string ip, int port);
        Task SendPacket(Packet packet, string ip, int port);
        event EventHandler<EventArgs> MessageNotSend;
    }
}