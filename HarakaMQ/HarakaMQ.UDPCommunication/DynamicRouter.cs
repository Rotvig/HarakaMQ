using System;
using System.Linq;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using Newtonsoft.Json;
using SimpleInjector;

namespace HarakaMQ.UDPCommunication
{
    public class DynamicRouter : IDynamicRouter
    {
        private HarakaMQUDPConfiguration _harakaMqudpConfiguration;
        private IAutomaticRepeatReQuest _automaticRepeatReqeust;
        public event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        public event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        public event EventHandler<MessageReceivedEventArgs> Subscribe;
        public event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        public event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        public event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;
        
        public void Send(Message msg, string topic)
        {
            _automaticRepeatReqeust.Send(msg, GetBroker(), topic);
        }

        public void SendAdministrationMessage(AdministrationMessage msg)
        {
            _automaticRepeatReqeust.SendAdministrationMessage(msg, GetBroker());
        }

        public void SendPackage(Packet packet)
        {
            _automaticRepeatReqeust.SendPacket(packet, GetBroker());
        }

        public void SetupConnection(HarakaMQUDPConfiguration harakaMqudpConfiguration)
        {
            _harakaMqudpConfiguration = harakaMqudpConfiguration;
            Setup.SetupDi(harakaMqudpConfiguration);
            _automaticRepeatReqeust = Setup.container.GetInstance<IAutomaticRepeatReQuest>();
            _automaticRepeatReqeust.PublishPackage += PublishPackage;
            _automaticRepeatReqeust.Subscribe += Subscribe;
            _automaticRepeatReqeust.Unsubscribe += Unsubscribe;
            _automaticRepeatReqeust.QueueDeclare += QueueDeclare;
            _automaticRepeatReqeust.AntiEntropyMessage += AntiEntropyMessage;
            _automaticRepeatReqeust.ClockSyncMessage += ClockSyncMessage;
            _automaticRepeatReqeust.Listen(harakaMqudpConfiguration.ListenPort);
        }

        private Broker GetBroker()
        {
            if (_harakaMqudpConfiguration.Brokers.Any())
            {
                return _harakaMqudpConfiguration.Brokers.First();
            }
            throw new ArgumentException($"UDP configuration did not contain any brokers {JsonConvert.SerializeObject(_harakaMqudpConfiguration)}");
        }
    }
}