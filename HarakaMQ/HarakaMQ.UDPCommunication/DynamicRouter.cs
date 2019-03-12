using System;
using System.Linq;
using System.Threading.Tasks;
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
        private IHarakaMQUDPConfiguration _harakaMqudpCopnfiguration;
        private IAutomaticRepeatReQuest _automaticRepeatReqeust;
        public event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        public event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        public event EventHandler<MessageReceivedEventArgs> Subscribe;
        public event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        public event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        public event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;
        
        public void Send(Message msg, string topic, Broker broker = null)
        {
            _automaticRepeatReqeust.Send(msg, topic, broker ?? GetBroker());
        }

        public void SendAdministrationMessage(AdministrationMessage msg, Broker broker = null)
        {
            _automaticRepeatReqeust.SendAdministrationMessage(msg, broker ??  GetBroker());
        }

        public async Task SendPacket(Packet packet, Broker broker = null)
        {
            await _automaticRepeatReqeust.SendPacket(packet, broker ?? GetBroker());
        }

        public void SetupConnection(IHarakaMQUDPConfiguration harakaMqudpConfiguration)
        {
            _harakaMqudpCopnfiguration = harakaMqudpConfiguration;
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
            if (_harakaMqudpCopnfiguration.Brokers.Any())
            {
                return _harakaMqudpCopnfiguration.Brokers.First();
            }
            throw new ArgumentException($"UDP configuration did not contain any brokers {JsonConvert.SerializeObject(_harakaMqudpCopnfiguration)}");
        }
    }
}