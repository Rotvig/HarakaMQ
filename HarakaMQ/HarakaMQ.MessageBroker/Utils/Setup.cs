using HarakaMQ.DB;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleInjector;

namespace HarakaMQ.MessageBroker.Utils
{
    public static class Setup
    {
        internal static Container container;
        internal static string PublisherCs = "Publishers";
        public static int AntiEntropySize = 300;
        public static int PacketSize = 65000;
        public static int TotalPacketSize = PacketSize + AntiEntropySize;

        internal static void Initialize(IHarakaMQUDPConfiguration harakaMQUDPConfiguration, IHarakaMQMessageBrokerConfiguration harakaMqMessageBrokerConfiguration, IConfiguration configuration)
        {
            container = new Container();
            ISerializer serializer = null;
            ILoggerFactory loggerFactory = null;
            if (harakaMQUDPConfiguration.Logging.LogLevel.Default.ToLower() == "debug")
            {
                loggerFactory = LoggerFactory.Create(builder => 
                    builder
                        .AddConfiguration(configuration)
                        .AddConsole());
                serializer = new HarakaUTF8JsonSerializer();
            }
            else
            {
                loggerFactory = LoggerFactory.Create(builder => 
                    builder
                        .AddConfiguration(configuration)
                        .AddConsole());
                serializer = new HarakaMessagePackSerializer();
            }
            container.Register(() => loggerFactory, Lifestyle.Singleton);
            container.RegisterConditional(
                typeof(ILogger<>),
                c => typeof(Logger<>).MakeGenericType(c.Consumer.ImplementationType),
                Lifestyle.Singleton,
                c => true);
            container.Register(() => serializer, Lifestyle.Singleton);
            container.Register(() => harakaMqMessageBrokerConfiguration, Lifestyle.Singleton);
            container.Register(() => harakaMQUDPConfiguration, Lifestyle.Singleton);
            container.Register<ISmartQueueFactory, SmartQueueFactory>(Lifestyle.Singleton);
            container.Register<IUdpCommunication, UdpCommunication>(Lifestyle.Singleton);
            container.Register<IMergeProcedure, MergeProcedure>(Lifestyle.Singleton);
            container.Register<ISchedular, Schedular>(Lifestyle.Singleton);
            container.Register<IAntiEntropy, AntiEntropy>(Lifestyle.Singleton);
            container.Register<IGossip, PingPong>(Lifestyle.Singleton);
            container.Register<IHarakaDb>(() => new HarakaDb(serializer, "Topics", PublisherCs), Lifestyle.Singleton);
            container.Register<IPersistenceLayer>(() => new PersistenceLayer(container.GetInstance<IHarakaDb>(), "Topics"), Lifestyle.Singleton);
            container.Register<ITimeSyncProtocol, NTP>(Lifestyle.Singleton);
            container.Verify();
        }
    }
}