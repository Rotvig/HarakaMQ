using System;
using System.Linq;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using SimpleInjector;

namespace HarakaMQ.UDPCommunication
{
    public class UdpCommunication : IUdpCommunication
    {
        private IAutomaticRepeatReQuest _automaticRepeatReqeust;
        private HarakaMQUDPConfiguration _harakaMqudpConfiguration;

        public event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        public event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        public event EventHandler<MessageReceivedEventArgs> Subscribe;
        public event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        public event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        public event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;

        public void Listen(HarakaMQUDPConfiguration harakaMqudpConfiguration)
        {
            _harakaMqudpConfiguration = harakaMqudpConfiguration;
            Setup.SetupDi();
            _automaticRepeatReqeust = Setup.container.GetInstance<IAutomaticRepeatReQuest>();
            Setup.container.Register(() => harakaMqudpConfiguration, Lifestyle.Singleton);

            _automaticRepeatReqeust.PublishPackage += PublishPackage;
            _automaticRepeatReqeust.Subscribe += Subscribe;
            _automaticRepeatReqeust.Unsubscribe += Unsubscribe;
            _automaticRepeatReqeust.QueueDeclare += QueueDeclare;
            _automaticRepeatReqeust.AntiEntropyMessage += AntiEntropyMessage;
            _automaticRepeatReqeust.ClockSyncMessage += ClockSyncMessage;
            _automaticRepeatReqeust.Listen(harakaMqudpConfiguration.ListenPort);
        }

        public void Send(Message msg, string topic)
        {
            _automaticRepeatReqeust.Send(msg, topic);
        }

        public void Send(Message msg, string ip, int port, string topic)
        {
            _automaticRepeatReqeust.Send(msg, ip, port, topic);
        }

        public async void SendPackage(Packet packet, string ip, int port)
        {
            await _automaticRepeatReqeust.SendPacket(packet, ip, port);
        }

        public void SendAdministrationMessage(AdministrationMessage msg)
        {
            _automaticRepeatReqeust.SendAdministrationMessage(msg);
        }

        public void SendAdministrationMessage(AdministrationMessage msg, string ip, int port)
        {
            _automaticRepeatReqeust.SendAdministrationMessage(msg, ip, port);
        }
    }
}