using HarakaMQ.DB;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
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

        internal static void Initialize()
        {
            // 1. Create a new Simple Injector container
            container = new Container();

            // 2. Configure the container (register)
            var serializer = new UTF8JsonSerializer();
            container.Register<ISerializer>(() => serializer, Lifestyle.Singleton);
            container.Register<ISmartQueueFactory, SmartQueueFactory>(Lifestyle.Singleton);
            container.Register<IJsonConfigurator, JsonConfigurator>(Lifestyle.Singleton);
            container.Register<IUdpCommunication, UdpCommunication>(Lifestyle.Singleton);
            container.Register<IMergeProcedure, MergeProcedure>(Lifestyle.Singleton);
            container.Register<ISchedular, Schedular>(Lifestyle.Singleton);
            container.Register<IAntiEntropy, AntiEntropy>(Lifestyle.Singleton);
            container.Register<IGossip, PingPong>(Lifestyle.Singleton);
            container.Register<IHarakaDb>(() => new HarakaDb(serializer,"Topics", PublisherCs), Lifestyle.Singleton);
            container.Register<IPersistenceLayer>(() => new PersistenceLayer(container.GetInstance<IHarakaDb>(), "Topics"), Lifestyle.Singleton);
            container.Register<IClock, Clock>(Lifestyle.Singleton);
            container.Register<ITimeSyncProtocol, NTP>(Lifestyle.Singleton);

            // 3. Verify your configuration: Only for testing
            container.Verify();
        }
    }
}