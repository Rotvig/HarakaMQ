using System;

namespace HarakaMQ.MessageBroker.NET461.Models
{
    public class BrokerInformation : Broker
    {
        public BrokerInformation()
        {
            Active = true;
        }

        public bool Active { get; set; }
        public int CurrentAntiEntropyRound { get; set; }
        public int LastCommittedSeqNumberReceived { get; set; }
        public Guid AntiEntropyRoundScheduledTaskIdResponse { get; set; }
        public Guid AntiEntropyRoundScheduledTaskIdAnswer { get; set; }


        public void DeactivateBroker()
        {
            Active = false;
            AntiEntropyRoundScheduledTaskIdAnswer = Guid.Empty;
        }
    }
}