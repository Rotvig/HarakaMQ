using System.Collections.Generic;
using System.Diagnostics;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication.Utils
{
    public class DefaultHarakaMQUDPConfiguration : IHarakaMQUDPConfiguration
    {
        public int AcknowledgeMessageAfterNumberOfMessages { get; set; } = 100;
        public int DelayedAcknowledgeWaitTimeInMiliseconds { get; set; } = 500;
        public bool DisableIPv4Fragmentation { get; set; } = false;
        public IEnumerable<string> NoDelayedAcknowledgeClientIds { get; set; } = new List<string>();
        public int ListenPort { get; set; } = 12000;
        public string Ip { get; set; } = Ext.GetIp4Address();
        public IEnumerable<Broker> Brokers { get; set; } = new List<Broker>();
        public Logging Logging { get; set; } = new Logging(new LogLevel(){Default = "debug"});

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

    public interface IHarakaMQUDPConfiguration
    {
        int AcknowledgeMessageAfterNumberOfMessages { get; set; }
        int DelayedAcknowledgeWaitTimeInMiliseconds { get; set; }
        bool DisableIPv4Fragmentation { get; set; }
        IEnumerable<string> NoDelayedAcknowledgeClientIds { get; set; }
        int ListenPort { get; set; }
        string Ip { get; set; }
        IEnumerable<Broker> Brokers { get; set; }
        Logging Logging { get; set; }   
    }
}