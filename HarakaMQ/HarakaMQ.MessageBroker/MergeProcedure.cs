using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker
{
    public class MergeProcedure : IMergeProcedure
    {
        public List<PublishPacketReceivedEventArgs> MergeMessages(IEnumerable<PublishPacketReceivedEventArgs> tentativMessages1, IEnumerable<PublishPacketReceivedEventArgs> tentativMessages2)
        {
            //MERGE TENTATIVE MESSAGES and return new merged list
            return tentativMessages1.Concat(tentativMessages2).OrderBy(x => x.Packet.ReceivedAtBroker.Value).ToList();
        }

        public void CommitStableMessages(ref List<PublishPacketReceivedEventArgs> committedMessages, ref List<PublishPacketReceivedEventArgs> foreignTentativeMessages, ref List<PublishPacketReceivedEventArgs> ownTentativeMessages, ref int lastAntiEntropyCommit, ref int globalSequenceNumber)
        {
            //COMMIT MESSAGES AND REMOVE COMMITED MESSAGES FROM TENTATIVE
            var tentativeMessages = foreignTentativeMessages.Concat(ownTentativeMessages).ToList();
            var lastCommit = lastAntiEntropyCommit + 3; //Commits are stable after atleast 3 rounds
            foreach (var messageReceivedEventArgse in tentativeMessages)
            {
                if (!messageReceivedEventArgse.Packet.AntiEntropyRound.HasValue || !(messageReceivedEventArgse.Packet.AntiEntropyRound <= lastCommit)) continue;
                globalSequenceNumber++;
                messageReceivedEventArgse.Packet.GlobalSequenceNumber = globalSequenceNumber;
                committedMessages.Add(messageReceivedEventArgse);
                foreignTentativeMessages.Remove(messageReceivedEventArgse);
                ownTentativeMessages.Remove(messageReceivedEventArgse);
                Debug.WriteLine("Committed Packet - Number: " + messageReceivedEventArgse.Packet.SeqNo + " Time: " + messageReceivedEventArgse.Packet.ReceivedAtBroker);
            }
            lastAntiEntropyCommit = lastAntiEntropyCommit + 3; //Update 
        }
    }
}