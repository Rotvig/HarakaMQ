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

        public void Send(Message msg, string topic)
        {
            _dynamicRouter.Send(msg, topic);
        }

        public void Send(Message msg, string ip, int port, string topic)
        {
            _dynamicRouter.Send(msg, topic);
        }

        public async void SendPackage(Packet packet, string ip, int port)
        {
            await _dynamicRouter.SendPacket(packet);
        }

        public void SendAdministrationMessage(AdministrationMessage msg)
        {
            _dynamicRouter.SendAdministrationMessage(msg);
        }

        public void SendAdministrationMessage(AdministrationMessage msg, string ip, int port)
        {
            _dynamicRouter.SendAdministrationMessage(msg);
        }
    }
}