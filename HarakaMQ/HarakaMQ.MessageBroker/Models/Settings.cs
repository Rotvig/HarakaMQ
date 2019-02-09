using System.Collections.Generic;

namespace HarakaMQ.MessageBroker.Models
{
    public class Settings
    {
        public int BrokerPort { get; set; }
        public int PrimaryNumber { get; set; }
        public int AntiEntropyMilliseonds { get; set; }
        public bool RunInClusterSetup { get; set; }
        public List<Broker> Brokers { get; set; }
        public string TimeSyncServerAddress { get; set; }
    }
}