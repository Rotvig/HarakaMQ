using System;
using System.Linq;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.UDPCommunication
{
    public class UdpCommunication : IUdpCommunication
    {
        private IAutomaticRepeatReQuest _automaticRepeatReqeust;
        private string _ip;
        private int _port;

        public event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        public event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        public event EventHandler<MessageReceivedEventArgs> Subscribe;
        public event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        public event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        public event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;

        public void Listen(int port)
        {
            Setup.Ip = Ext.GetIp4Address();
            Setup.Port = port;
            Setup.SetupDi();
            _automaticRepeatReqeust = Setup.container.GetInstance<IAutomaticRepeatReQuest>();

            _automaticRepeatReqeust.PublishPackage += PublishPackage;
            _automaticRepeatReqeust.Subscribe += Subscribe;
            _automaticRepeatReqeust.Unsubscribe += Unsubscribe;
            _automaticRepeatReqeust.QueueDeclare += QueueDeclare;
            _automaticRepeatReqeust.AntiEntropyMessage += AntiEntropyMessage;
            _automaticRepeatReqeust.ClockSyncMessage += ClockSyncMessage;
            _automaticRepeatReqeust.Listen(port);
        }

        public void SetBrokerInformation(string ip, int port)
        {
            _port = port;
            _ip = ip;
        }

        public void SetUpUdpComponent(int ackAfterNumOfMessages, int delayedAckWaitTime, bool dontFragment = false, params string[] noDelayedAckClientIds)
        {
            Setup.AckAfterNumber = ackAfterNumOfMessages;
            Setup.DelayedAckWaitTime = delayedAckWaitTime;
            Setup.NoDelayedAckClients = noDelayedAckClientIds.ToList();
            Setup.DontFragment = dontFragment;
        }

        public void Send(Message msg, string topic)
        {
            _automaticRepeatReqeust.Send(msg, _ip, _port, topic);
        }

        public void Send(Message msg, string ip, int port, string topic)
        {
            _automaticRepeatReqeust.Send(msg, ip, port, topic);
        }

        public void SendPackage(Packet packet, string ip, int port)
        {
            _automaticRepeatReqeust.SendPacket(packet, ip, port);
        }

        public void SendAdministrationMessage(AdministrationMessage msg)
        {
            _automaticRepeatReqeust.SendAdministrationMessage(msg, _ip, _port);
        }

        public void SendAdministrationMessage(AdministrationMessage msg, string ip, int port)
        {
            _automaticRepeatReqeust.SendAdministrationMessage(msg, ip, port);
        }
    }
}