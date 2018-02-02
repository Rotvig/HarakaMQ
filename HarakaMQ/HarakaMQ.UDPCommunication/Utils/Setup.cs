using System.Collections.Generic;
using HarakaMQ.DB;
using HarakaMQ.UDPCommunication.Interfaces;
using SimpleInjector;

namespace HarakaMQ.UDPCommunication.Utils
{
    public static class Setup
    {
        public static Container container;

        public static int MaxPayloadSize = 60203; // 65,507-4 (4 is for the enum added at the senderMessage)

        public static List<string> NoDelayedAckClients = new List<string>();
        public static string ClientId { get; private set; }
        public static bool UseDelayedAck { get; set; }
        public static int WaitTimeOnDelayedAckInMiliseconds { get; set; }
        public static string OutgoingMessagesCS { get; set; }
        public static string IngoingMessagesCS { get; set; }
        public static string ClientsCS { get; set; }
        public static int Port { get; set; }
        public static string Ip { get; set; }
        public static int DelayedAckWaitTime { get; set; }
        public static int AckAfterNumber { get; set; }
        public static bool DontFragment { get; set; }

        public static void SetupDi()
        {
            ClientId = Ip + Port;
            OutgoingMessagesCS = "OutgoingMessages_" + ClientId;
            IngoingMessagesCS = "InGoingMessages_" + ClientId;
            ClientsCS = "Clients_" + ClientId;
            // 1. Create a new Simple Injector container
            container = new Container();

            // 2. Configure the container (register)
            container.Register<ISchedular, Schedular>(Lifestyle.Singleton);
            container.Register<ISender, Sender>(Lifestyle.Singleton);
            container.Register<IReceiver, Receiver>(Lifestyle.Singleton);
            container.Register<IAutomaticRepeatReQuest, AutomaticRepeatReQuest>(Lifestyle.Transient);
            container.Register<IGuranteedDelivery, GuranteedDelivery>(Lifestyle.Singleton);
            container.Register<IIdempotentReceiver, IdempotentReceiver>(Lifestyle.Singleton);
            container.Register<IHarakaDb>(() => new HarakaDb(OutgoingMessagesCS, IngoingMessagesCS, ClientsCS),
                Lifestyle.Singleton);

            // 3. Verify your configuration: Only for testing
            container.Verify();
        }
    }
}