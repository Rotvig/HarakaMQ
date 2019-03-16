using System.Collections.Generic;
using HarakaMQ.MessageBroker.Interfaces;

namespace HarakaMQ.MessageBroker
{
    public class HarakaMQMessageBrokerConfiguration : IHarakaMQMessageBrokerConfiguration
    {
        public int BrokerPort { get; set; }
        public int PrimaryNumber { get; set; }
        public int AntiEntropyMilliseonds { get; set; }
        public bool RunInClusterSetup { get; set; }
        public List<Models.MessageBroker> MessageBrokers { get; set; }
        public string TimeSyncServerAddress { get; set; }
    }
}