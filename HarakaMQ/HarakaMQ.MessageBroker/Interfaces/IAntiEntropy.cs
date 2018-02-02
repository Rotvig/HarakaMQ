using System.Collections.Generic;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.UDPCommunication.Events;

namespace HarakaMQ.MessageBroker.Interfaces
{
    public interface IAntiEntropy : IAntitropyMethods
    {
        List<PublishPacketReceivedEventArgs> GetTentativeMessagesToSendForNonPrimaryBroker(ref int numberOfBytesUsed, int currentAntiEntropyRound);
        List<PublishPacketReceivedEventArgs> GetCommittedMessagesToSend(ref int numberOfBytesUsed, int offset);
        List<PublishPacketReceivedEventArgs> GetTentativeMessagesToSendForPrimaryBroker(ref int numberOfBytesUsed, int currentAntiEntropyRound);
        List<Subscriber> GetSubscribers();
        List<Publisher> GetPublishers();
        void AntiEntropyNonPrimaryMessageReceived(AntiEntropyMessage message);
        void AntiEntropyAddTentativeMessages(AntiEntropyMessage antiEntropyMessage, ref int currentAntiEntropyRound, ref int globalSequenceNumber);
        void AntiEntropyCommitStableMessages(ref int lastAntiEntropyCommit, ref int globalSequenceNumber);
        void GarbageCollectMessages(int messageGlobalSequenceNumber);
    }
}