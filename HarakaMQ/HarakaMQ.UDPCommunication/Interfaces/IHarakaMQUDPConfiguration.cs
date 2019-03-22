using System.Collections.Generic;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IHarakaMQUDPConfiguration
    {
        int AcknowledgeMessageAfterNumberOfMessages { get; set; }
        int DelayedAcknowledgeWaitTimeInMiliseconds { get; set; }
        bool DisableIPv4Fragmentation { get; set; } 
        IEnumerable<string> DisableDelayedAcknowledgeForClientWithIds { get; set; }
        int ListenPort { get; set; }
        string IpAdress { get; set; }
        IEnumerable<Host> Hosts { get; set; }
        Logging Logging { get; set; }
    }
}