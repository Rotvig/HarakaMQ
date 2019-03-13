using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Utils;
using Microsoft.Extensions.Configuration;
using Setup = HarakaMQ.MessageBroker.Utils.Setup;

namespace HarakaMQ.MessageBroker
{
    internal class Program
    {
        private static IUdpCommunication _udpCommunication;
        private static IGossip _gossip;

        private static void Main(string[] args)
        {
            Debug.WriteLine("Initializing HarakaMQ");
            var configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            
            Initialize(configurationRoot);
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static void Initialize(IConfigurationRoot configurationRoot)
        {
            var harakaUdpConfiguration = new HarakaMQUDPConfiguration();
            var harakaMessageBrokerConfiguration = new HarakaMqMessageBrokerConfiguration();
            configurationRoot.Bind(harakaUdpConfiguration);
            configurationRoot.Bind(harakaMessageBrokerConfiguration);

            harakaUdpConfiguration.DisableDelayedAcknowledgeForClientWithIds = harakaUdpConfiguration.Brokers.Select(broker => broker.Id).ToList();
            Setup.Initialize();
            _udpCommunication = Setup.container.GetInstance<IUdpCommunication>();
            _udpCommunication.QueueDeclare += QueueDeclareMessageRecieved;
            _udpCommunication.PublishPackage += PublishMessageRecieved;
            _udpCommunication.Subscribe += SubsribeMessageRecieved;
            _udpCommunication.AntiEntropyMessage += AntiEntropyMessageMessageReceived;
            _udpCommunication.Listen(harakaUdpConfiguration);
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