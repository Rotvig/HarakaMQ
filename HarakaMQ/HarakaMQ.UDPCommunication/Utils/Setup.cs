using System.Collections.Generic;
using HarakaMQ.DB;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication.Interfaces;
using Microsoft.Extensions.Logging;
using SimpleInjector;

namespace HarakaMQ.UDPCommunication.Utils
{
    public static class Setup
    {
        public static Container container;

        public const int MaxPayloadSize = 60203; // 65,507-4 (4 is for the enum added at the senderMessage)
        public static string OutgoingMessagesCS { get; set; }
        public static string IngoingMessagesCS { get; set; }
        public static string ClientsCS { get; set; }
        
        public static void SetupDi(IHarakaMQUDPConfiguration harakaMqudpCopnfiguration)
        {
            var clientId = harakaMqudpCopnfiguration.IpAdress + harakaMqudpCopnfiguration.ListenPort;
            OutgoingMessagesCS = "OutgoingMessages_" + clientId;
            IngoingMessagesCS = "InGoingMessages_" + clientId;
            ClientsCS = "Clients_" + clientId;
            // 1. Create a new Simple Injector container
            container = new Container();

            // 2. Configure the container (register)
            container.Register(() => harakaMqudpCopnfiguration, Lifestyle.Singleton);

            ISerializer serializer = null;
            ILoggerFactory loggerFactory = null;
            if (harakaMqudpCopnfiguration.Logging.LogLevel.Default.ToLower() == "debug")
            {
                loggerFactory = LoggerFactory.Create(builder => 
                    builder
                        .AddConsole());
                serializer = new HarakaUTF8JsonSerializer();
                container.Register(() => serializer, Lifestyle.Singleton);
            }
            else
            {
                loggerFactory = LoggerFactory.Create(builder => 
                    builder
                        .AddConsole());
                serializer = new HarakaMessagePackSerializer();
                container.Register(() => serializer, Lifestyle.Singleton);
            }

            container.Register(() => loggerFactory, Lifestyle.Singleton);
            container.RegisterConditional(
                typeof(ILogger<>),
                c => typeof(Logger<>).MakeGenericType(c.Consumer.ImplementationType),
                Lifestyle.Singleton,
                c => true);
            container.Register(() => harakaMqudpCopnfiguration, Lifestyle.Singleton);
            container.Register(() => serializer, Lifestyle.Singleton);
            container.Register<ISchedular, Schedular>(Lifestyle.Singleton);
            container.Register<ISender, Sender>(Lifestyle.Singleton);
            container.Register<IReceiver, Receiver>(Lifestyle.Singleton);
            container.Register<IDynamicRouter, DynamicRouter>(Lifestyle.Singleton);
            container.Register<IAutomaticRepeatReQuest, AutomaticRepeatReQuest>(Lifestyle.Singleton);
            container.Register<IGuranteedDelivery, GuranteedDelivery>(Lifestyle.Singleton);
            container.Register<IIdempotentReceiver, IdempotentReceiver>(Lifestyle.Singleton);
            container.Register<IHarakaDb>(() => new HarakaDb(serializer, OutgoingMessagesCS, IngoingMessagesCS, ClientsCS), Lifestyle.Singleton);

            // 3. Verify your configuration: Only for testing
            container.Verify();
        }
    }
}
