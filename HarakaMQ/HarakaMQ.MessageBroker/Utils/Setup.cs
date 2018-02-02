using HarakaMQ.DB;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using SimpleInjector;

namespace HarakaMQ.MessageBroker.Utils
{
    internal static class Setup
    {
        internal static Container container;
        internal static string PublisherCs = "Publishers";
        internal static int AntiEntropySize = 300;

        public static void Initialize()
        {
            // 1. Create a new Simple Injector container
            container = new Container();

            // 2. Configure the container (register)
            container.Register<IJsonConfigurator, JsonConfigurator>(Lifestyle.Singleton);
            container.Register<IUdpCommunication, UdpCommunication>(Lifestyle.Singleton);
            container.Register<IMergeProcedure, MergeProcedure>(Lifestyle.Singleton);
            container.Register<ISchedular, Schedular>(Lifestyle.Singleton);
            container.Register<IAntiEntropy, AntiEntropy>(Lifestyle.Singleton);
            container.Register<IGossip, PingPong>(Lifestyle.Singleton);
            container.Register<IHarakaDb>(() => new HarakaDb("Topics", PublisherCs), Lifestyle.Singleton);
            container.Register<IPersistenceLayer>(() => new PersistenceLayer(container.GetInstance<IHarakaDb>(), "Topics"), Lifestyle.Transient);
            container.Register<IClock, Clock>(Lifestyle.Singleton);

            // 3. Verify your configuration: Only for testing
            container.Verify();
        }
    }
}