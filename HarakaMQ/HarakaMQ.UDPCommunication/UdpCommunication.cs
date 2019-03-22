using System;
using System.Linq;
using System.Net;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using SimpleInjector;

namespace HarakaMQ.UDPCommunication
{
    public class UdpCommunication : IUdpCommunication
    {
        private IDynamicRouter _dynamicRouter;
        private IHarakaMQUDPConfiguration _harakaMqudpCopnfiguration;

        public event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        public event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        public event EventHandler<MessageReceivedEventArgs> Subscribe;
        public event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        public event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        public event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;

        public void Listen(IHarakaMQUDPConfiguration harakaMqudpConfiguration)
        {
            _harakaMqudpCopnfiguration = harakaMqudpConfiguration;
            Setup.SetupDi(_harakaMqudpCopnfiguration);
            _dynamicRouter = Setup.container.GetInstance<IDynamicRouter>();

            _dynamicRouter.PublishPackage += PublishPackage;
            _dynamicRouter.Subscribe += Subscribe;
            _dynamicRouter.Unsubscribe += Unsubscribe;
            _dynamicRouter.QueueDeclare += QueueDeclare;
            _dynamicRouter.AntiEntropyMessage += AntiEntropyMessage;
            _dynamicRouter.ClockSyncMessage += ClockSyncMessage;
            _dynamicRouter.SetupConnection(_harakaMqudpCopnfiguration);
        }

        public void Send(Message msg, string topic, Host host = null)
        {
            _dynamicRouter.Send(msg, topic, host);
        }

        public async void SendPackage(Packet packet, Host host = null)
        {
            await _dynamicRouter.SendPacket(packet, host);
        }

        public void SendAdministrationMessage(AdministrationMessage msg, Host host = null)
        {
            _dynamicRouter.SendAdministrationMessage(msg, host);
        }
    }
}