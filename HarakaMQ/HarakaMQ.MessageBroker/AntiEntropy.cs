using System.Collections.Generic;
using System.Linq;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker.Events;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;

namespace HarakaMQ.MessageBroker
{
    public class AntiEntropy : IAntiEntropy
    {
        private readonly IHarakaDb _harakaDb;
        private readonly IJsonConfigurator _jsonConfigurator;
        private readonly IMergeProcedure _mergeProcedure;
        private readonly List<Publisher> _publishers = new List<Publisher>();
        private readonly List<ISmartQueue> _queues = new List<ISmartQueue>();
        private volatile List<Subscriber> _subscribers = new List<Subscriber>();
        private List<PublishPacketReceivedEventArgs> CommittedMessages = new List<PublishPacketReceivedEventArgs>();
        private List<PublishPacketReceivedEventArgs> ForeignTentativeMessages = new List<PublishPacketReceivedEventArgs>();
        private List<PublishPacketReceivedEventArgs> OwnTentativeMessages = new List<PublishPacketReceivedEventArgs>();
        private object _messagesLock = new object();

        public AntiEntropy(IHarakaDb harakaDb, IMergeProcedure mergeProcedure, IJsonConfigurator jsonConfigurator)
        {
            _harakaDb = harakaDb;
            _mergeProcedure = mergeProcedure;
            _jsonConfigurator = jsonConfigurator;

            //Initialize all existing queues on startup
            foreach (var topic in _harakaDb.TryGetObjects<Topic>("Topics"))
            {
                var smartQueue = new SmartQueue(Setup.container.GetInstance<IUdpCommunication>(), Setup.container.GetInstance<IPersistenceLayer>(), Setup.container.GetInstance<IJsonConfigurator>(), topic);
                _queues.Add(smartQueue);
                smartQueue.SubscribersHasBeenUpdated += SmartQueueOnSubscribersHasBeenUpdated;
            }

            //Fetch all pub
            //Merge tentative
        }

        public List<PublishPacketReceivedEventArgs> GetTentativeMessagesToSendForNonPrimaryBroker(ref int numberOfBytesUsed, int currentAntiEntropyRound)
        {
            var tentativeMessagesToSend = new List<PublishPacketReceivedEventArgs>();
            lock (_messagesLock)
            {
                foreach (var messageReceivedEventArgse in OwnTentativeMessages.Concat(ForeignTentativeMessages))
                {
                    if (messageReceivedEventArgse.Packet.AntiEntropyRound.HasValue) continue;

                    if (messageReceivedEventArgse.Packet.Size + numberOfBytesUsed <= Setup.TotalPacketSize)
                    {
                        messageReceivedEventArgse.Packet.AntiEntropyRound = currentAntiEntropyRound;
                        tentativeMessagesToSend.Add(messageReceivedEventArgse);
                        numberOfBytesUsed += messageReceivedEventArgse.Packet.Size;
                    }
                    else
                    {
                        return tentativeMessagesToSend;
                    }
                }
            }

            return tentativeMessagesToSend;
        }

        public List<PublishPacketReceivedEventArgs> GetCommittedMessagesToSend(ref int numberOfBytesUsed, int globalSequenceNumberOffset)
        {
            //return CommittedMessages.Where(x => x.Packet.GlobalSequenceNumber >= offset && x.Packet.GlobalSequenceNumber < offset + numberOfMessagesToFetch).ToList();
            var committedMessagesToSend = new List<PublishPacketReceivedEventArgs>();
            lock (_messagesLock)
            {
                foreach (var publishPackageReceivedEventArgse in CommittedMessages.Where(x => x.Packet.GlobalSequenceNumber >= globalSequenceNumberOffset))
                {
                    if (publishPackageReceivedEventArgse.Packet.Size + numberOfBytesUsed <= Setup.TotalPacketSize)
                    {
                        committedMessagesToSend.Add(publishPackageReceivedEventArgse);
                        numberOfBytesUsed += publishPackageReceivedEventArgse.Packet.Size;
                    }
                    else
                    {
                        return committedMessagesToSend;
                    }
                }
            }
            return committedMessagesToSend;
        }

        public void AntiEntropyNonPrimaryMessageReceived(AntiEntropyMessage message)
        {
            //Todo: Store all new publishers
            MergeTentativeMessagesAndPassThemToTheSmartQueues(message);
            MergeCommittedMessagesAndPassThemToSmartQueues(message);
        }

        public List<PublishPacketReceivedEventArgs> GetTentativeMessagesToSendForPrimaryBroker(ref int numberOfBytesUsed, int currentAntiEntropyRound)
        {
            var tentativeMessagesToSend = new List<PublishPacketReceivedEventArgs>();
            lock (_messagesLock)
            {
                foreach (var messageReceivedEventArgse in OwnTentativeMessages)
                {
                    if (messageReceivedEventArgse.Packet.AntiEntropyRound.HasValue) continue;

                    if (messageReceivedEventArgse.Packet.Size + numberOfBytesUsed <= Setup.TotalPacketSize)
                    {
                        messageReceivedEventArgse.Packet.AntiEntropyRound = currentAntiEntropyRound;
                        tentativeMessagesToSend.Add(messageReceivedEventArgse);
                        numberOfBytesUsed += messageReceivedEventArgse.Packet.Size;
                    }
                    else
                    {
                        return tentativeMessagesToSend;
                    }
                }
            }
            return tentativeMessagesToSend;
        }

        public void AntiEntropyAddTentativeMessages(AntiEntropyMessage antiEntropyMessage, ref int currentAntiEntropyRound, ref int globalSequenceNumber)
        {
            MergeTentativeMessagesAndPassThemToTheSmartQueues(antiEntropyMessage);
        }

        public void AntiEntropyCommitStableMessages(ref int lastAntiEntropyCommit, ref int globalSequenceNumber)
        {
            //Commit Messages
            lock (_messagesLock)
            {
                _mergeProcedure.CommitStableMessages(ref CommittedMessages, ref ForeignTentativeMessages, ref OwnTentativeMessages, ref lastAntiEntropyCommit, ref globalSequenceNumber);
            }
            if (!CommittedMessages.Any()) return;

            foreach (var smartQueue in _queues)
            {
                var committedMessages = GetCommittedMessagesForSQ(smartQueue);
                smartQueue.AddEvent(new Event(committedMessages, EventType.ComittedMessages));
                smartQueue.UpdateLastGlobalSeqNoReceived(committedMessages.Last().Packet.GlobalSequenceNumber.Value);
            }
        }

        public void GarbageCollectMessages(int messageGlobalSequenceNumber)
        {
            lock (_messagesLock)
            {
                CommittedMessages.RemoveAll(x => x.Packet.GlobalSequenceNumber <= messageGlobalSequenceNumber);
            }
        }

        public void SubScribeMessageReceived(MessageReceivedEventArgs message)
        {
            var subscriber = new Subscriber(message.IpAddress, message.Port, _jsonConfigurator.GetSettings().PrimaryNumber, message.SenderClient);
            _queues.Find(x => x.GetTopicId() == message.AdministrationMessage.Topic).AddEvent(new Event(subscriber, EventType.AddSubscriber));
            _subscribers.Add(subscriber);
        }

        public void PublishMessageReceived(PublishPacketReceivedEventArgs message)
        {
            if (_jsonConfigurator.GetSettings().RunInClusterSetup)
            {
                if (!_publishers.Any(x => x.Ip == message.IpAddress && x.Port == message.Port))
                    lock (_harakaDb.GetLock(Setup.PublisherCs))
                    {
                        var pubs = _harakaDb.GetObjects<Publisher>(Setup.PublisherCs);
                        var publisher = new Publisher(message.IpAddress, message.Port, _jsonConfigurator.GetSettings().PrimaryNumber, message.SenderClient);
                        pubs.Add(publisher);
                        _harakaDb.StoreObject(Setup.PublisherCs, pubs);
                        _publishers.Add(publisher);
                    }

                _queues.Find(x => x.GetTopicId() == message.Packet.Topic).AddEvent(new Event(message, EventType.TentativeMessage));
                lock (_messagesLock)
                {
                    OwnTentativeMessages.Add(message);
                }
            }
            else
            {
                _queues.Find(x => x.GetTopicId() == message.Packet.Topic).AddEvent(new Event(message, EventType.ComittedMessage));
                lock (_messagesLock)
                {
                    CommittedMessages.Add(message);
                }
            }
        }

        public void QueueDeclareMessageReceived(MessageReceivedEventArgs message)
        {
            var topics = _harakaDb.TryGetObjects<Topic>("Topics");
            var topic = topics.Find(x => x.Name == message.AdministrationMessage.Topic);

            if (topic != null) return;

            lock (_harakaDb.GetLock("Topics"))
            {
                topics = _harakaDb.GetObjects<Topic>("Topics");
                topic = new Topic(message.AdministrationMessage.Topic);
                topics.Add(topic);
                _harakaDb.StoreObject("Topics", topics);
            }

            var smartQueue = new SmartQueue(Setup.container.GetInstance<IUdpCommunication>(), Setup.container.GetInstance<IPersistenceLayer>(), Setup.container.GetInstance<IJsonConfigurator>(), topic);
            smartQueue.SubscribersHasBeenUpdated += SmartQueueOnSubscribersHasBeenUpdated;
            _queues.Add(smartQueue);
        }

        public List<Subscriber> GetSubscribers()
        {
            return _subscribers;
        }

        public List<Publisher> GetPublishers()
        {
            return _publishers;
        }

        private List<PublishPacketReceivedEventArgs> GetCommittedMessagesForSQ(ISmartQueue smartQueue)
        {
            return CommittedMessages.Where(x => x.Packet.Topic == smartQueue.GetTopicId() && x.Packet.GlobalSequenceNumber > smartQueue.GetLastGlobalSeqReceived()).ToList();
        }

        private void SmartQueueOnSubscribersHasBeenUpdated(object sender, List<Subscriber> subscribers)
        {
            foreach (var subscriber in subscribers)
                _subscribers.Find(x => x.ClientId == subscriber.ClientId).UpdateGobalSequenceNumber(subscriber.GlobalSequenceNumberLastReceived);
        }

        private void MergeCommittedMessagesAndPassThemToSmartQueues(AntiEntropyMessage antiEntropyMessage)
        {
            if (!antiEntropyMessage.Committed.Any()) return;

            CommittedMessages.AddRange(antiEntropyMessage.Committed);
            foreach (var smartQueue in _queues)
                smartQueue.AddEvent(new Event(GetCommittedMessagesForSQ(smartQueue), EventType.ComittedMessages));
        }

        private void MergeTentativeMessagesAndPassThemToTheSmartQueues(AntiEntropyMessage antiEntropyMessage)
        {
            if (!antiEntropyMessage.Tentative.Any()) return;
            lock (_messagesLock)
            {
                ForeignTentativeMessages = _mergeProcedure.MergeMessages(antiEntropyMessage.Tentative, ForeignTentativeMessages);
            }

            foreach (var smartQueue in _queues)
            {
                smartQueue.AddEvent(new Event(ForeignTentativeMessages.Where(x => x.Packet.Topic == smartQueue.GetTopicId()).ToList(), EventType.TentativeMessages));
                smartQueue.AddEvent(new Event(antiEntropyMessage.Subscribers, EventType.AddSubscribers));
            }
        }
    }
}