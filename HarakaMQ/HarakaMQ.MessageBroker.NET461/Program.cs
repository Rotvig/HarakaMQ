using System;
using System.Diagnostics;
using System.Linq;
using HarakaMQ.MessageBroker.NET461.Interfaces;
using HarakaMQ.MessageBroker.NET461.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;

namespace HarakaMQ.MessageBroker.NET461
{
    internal class Program
    {
        private static IUdpCommunication _udpCommunication;
        private static IGossip _gossip;

        private static void Main(string[] args)
        {
            Initialize();
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static void Initialize()
        {
            Debug.WriteLine("Initializing HarakaMQ");
            Setup.Initialize();
            _udpCommunication = Setup.container.GetInstance<IUdpCommunication>();
            _udpCommunication.SetUpUdpComponent(10, 2000, false, Setup.container.GetInstance<IJsonConfigurator>().GetSettings().Brokers.Select(x => x.Ipaddress + x.Port).ToArray());
            _udpCommunication.QueueDeclare += QueueDeclareMessageRecieved;
            _udpCommunication.PublishPackage += PublishMessageRecieved;
            _udpCommunication.Subscribe += SubsribeMessageRecieved;
            _udpCommunication.AntiEntropyMessage += AntiEntropyMessageMessageReceived;
            _udpCommunication.Listen(Setup.container.GetInstance<IJsonConfigurator>().GetSettings().BrokerPort);
            _gossip = Setup.container.GetInstance<IGossip>();
            _gossip.StartGossip();
        }

        private static void AntiEntropyMessageMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _gossip.AntiEntropyMessageReceived(e);
        }

        private static void SubsribeMessageRecieved(object sender, MessageReceivedEventArgs e)
        {
            _gossip.SubScribeMessageReceived(e);
        }

        private static void PublishMessageRecieved(object sender, PublishPacketReceivedEventArgs e)
        {
            _gossip.PublishMessageReceived(e);
        }

        private static void QueueDeclareMessageRecieved(object sender, MessageReceivedEventArgs e)
        {
            _gossip.QueueDeclareMessageReceived(e);
        }
    }
}