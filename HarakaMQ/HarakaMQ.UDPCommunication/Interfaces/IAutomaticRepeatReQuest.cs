using System;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IAutomaticRepeatReQuest : IMessageEvents
    {
        void Listen(int port);
        void Send(Message msg, string ip, int port, string topic);
        void SendAdministrationMessage(AdministrationMessage msg, string ip, int port);
        void SendPacket(Packet packet, string ip, int port);
        event EventHandler<EventArgs> MessageNotSend;
    }
}