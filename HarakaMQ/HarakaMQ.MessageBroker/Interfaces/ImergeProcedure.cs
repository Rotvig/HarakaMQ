using System.Collections.Generic;
using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface IMergeProcedure
    {
        List<PublishPacketReceivedEventArgs> MergeMessages(IEnumerable<PublishPacketReceivedEventArgs> tentativMessages1, IEnumerable<PublishPacketReceivedEventArgs> tentativMessages2);
        void CommitStableMessages(ref List<PublishPacketReceivedEventArgs> committedMessages, ref List<PublishPacketReceivedEventArgs> foreignTentativeMessages, ref List<PublishPacketReceivedEventArgs> ownTentativeMessages, ref int lastAntiEntropyCommit, ref int globalSequenceNumber);
    }
}