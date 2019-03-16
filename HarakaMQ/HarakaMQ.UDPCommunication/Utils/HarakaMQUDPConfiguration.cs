using System.Collections.Generic;
using System.Diagnostics;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Utils
{
    public class HarakaMQUDPConfiguration : IHarakaMQUDPConfiguration
    {
        public int AcknowledgeMessageAfterNumberOfMessages { get; set; } = 100;
        public int DelayedAcknowledgeWaitTimeInMiliseconds { get; set; } = 2000;
        public bool DisableIPv4Fragmentation { get; set; } = false;
        public IEnumerable<string> DisableDelayedAcknowledgeForClientWithIds { get; set; } = new List<string>();
        public int ListenPort { get; set; } = 12000;
        public string IpAdress { get; set; } = Ext.GetIp4Address();
        public IEnumerable<Broker> Brokers { get; set; } = new List<Broker>();
        public Logging Logging { get; set; } = new Logging(new LogLevel() {Default = "debug"});

    }

    public class Logging
    {
        public Logging(LogLevel logLevel)
        {
            LogLevel = logLevel;
        }

        public LogLevel LogLevel { get; set; }
    }

    public class LogLevel
    {
        public string Default { get; set; }
        public string System { get; set; }
        public string Microsoft { get; set; }
    }
}