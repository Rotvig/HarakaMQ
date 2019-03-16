using System.Collections.Generic;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface IHarakaMQMessageBrokerConfiguration
    {
        int BrokerPort { get; set; }
        int PrimaryNumber { get; set; }
        int AntiEntropyMilliseonds { get; set; }
        bool RunInClusterSetup { get; set; }
        List<Models.MessageBroker> MessageBrokers { get; set; }
        string TimeSyncServerAddress { get; set; }
    }
}