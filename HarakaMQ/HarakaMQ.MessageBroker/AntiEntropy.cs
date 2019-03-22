using System.Collections.Generic;
using System.Linq;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker.Events;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.MessageBroker
{
    public class AntiEntropy : IAntiEntropy
    {
        private readonly IHarakaDb _harakaDb;
        private readonly ISmartQueueFactory _smartQueueFactory;
        private readonly IMergeProcedure _mergeProcedure;
        private readonly IHarakaMQMessageBrokerConfiguration _harakaMqMessageBrokerConfiguration;
        private readonly List<Publisher> _publishers = new List<Publisher>();
        private List<ISmartQueue> _queues;
        private volatile List<Subscriber> _subscribers = new List<Subscriber>();
        private List<PublishPacketReceivedEventArgs> _committedMessages = new List<PublishPacketReceivedEventArgs>();
        private List<PublishPacketReceivedEventArgs> _foreignTentativeMessages = new List<PublishPacketReceivedEventArgs>();
        private List<PublishPacketReceivedEventArgs> _ownTentativeMessages = new List<PublishPacketReceivedEventArgs>();
        private readonly object _messagesLock = new object();

        public AntiEntropy(IHarakaDb harakaDb, IMergeProcedure mergeProcedure, IHarakaMQMessageBrokerConfiguration harakaMqMessageBrokerConfiguration, ISmartQueueFactory smartQueueFactory)
        {
            _harakaDb = harakaDb;
            _mergeProcedure = mergeProcedure;
            _harakaMqMessageBrokerConfiguration = harakaMqMessageBrokerConfiguration;
            _smartQueueFactory = smartQueueFactory;
            Initialize();
            //Fetch all pub
            //Merge tentative
        }

        public void Initialize()
        {
            _queues = _smartQueueFactory.InitializeSmartQueues(SmartQueueOnSubscribersHasBeenUpdated);
        }

        public List<PublishPacketReceivedEventArgs> GetTentativeMessagesToSendForNonPrimaryBroker(ref int numberOfBytesUsed, int currentAntiEntropyRound)
        {
            var tentativeMessagesToSend = new List<PublishPacketReceivedEventArgs>();
            lock (_messagesLock)
            {
                foreach (var messageReceivedEventArgse in _ownTentativeMessages.Concat(_foreignTentativeMessages))
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
            var committedMessagesToSend = new List<PublishPacketReceivedEventArgs>();
            lock (_messagesLock)
            {
                foreach (var publishPackageReceivedEventArgs in _committedMessages.Where(x => x.Packet.GlobalSequenceNumber >= globalSequenceNumberOffset))
                {
                    if (publishPackageReceivedEventArgs.Packet.Size + numberOfBytesUsed <= Setup.TotalPacketSize)
                    {
                        committedMessagesToSend.Add(publishPackageReceivedEventArgs);
                        numberOfBytesUsed += publishPackageReceivedEventArgs.Packet.Size;
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

        public List<PublishPacketReceivedEventArgs> GetTentativeMessagesToSendForPrimaryBroker(int currentAntiEntropyRound)
        {
            var tentativeMessagesToSend = new List<PublishPacketReceivedEventArgs>();
            var numberOfBytesUsed = 0;
            lock (_messagesLock)
            {
                foreach (var messageReceivedEventArgs in _ownTentativeMessages)
                {
                    if (messageReceivedEventArgs.Packet.AntiEntropyRound.HasValue) continue;

                    if (messageReceivedEventArgs.Packet.Size + numberOfBytesUsed <= Setup.TotalPacketSize)
                    {
                        messageReceivedEventArgs.Packet.AntiEntropyRound = currentAntiEntropyRound;
                        tentativeMessagesToSend.Add(messageReceivedEventArgs);
                        numberOfBytesUsed += messageReceivedEventArgs.Packet.Size;
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
                _mergeProcedure.CommitStableMessages(ref _committedMessages, ref _foreignTentativeMessages, ref _ownTentativeMessages, ref lastAntiEntropyCommit, ref globalSequenceNumber);
            }
            if (!_committedMessages.Any()) return;

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
                _committedMessages.RemoveAll(x => x.Packet.GlobalSequenceNumber <= messageGlobalSequenceNumber);
            }
        }

        public void SubScribeMessageReceived(MessageReceivedEventArgs message)
        {
            var subscriber = new Subscriber(new Host { IPAddress = message.IpAddress, Port = message.Port}, _harakaMqMessageBrokerConfiguration.PrimaryNumber, message.SenderClient);
            _queues.Find(x => x.GetTopicId() == message.AdministrationMessage.Topic).AddEvent(new Event(subscriber, EventType.AddSubscriber));
            _subscribers.Add(subscriber);
        }

        public void PublishMessageReceived(PublishPacketReceivedEventArgs message)
        {
            if (_harakaMqMessageBrokerConfiguration.RunInClusterSetup)
            {
                if (!_publishers.Any(x => x.Ip == message.IpAddress && x.Port == message.Port))
                    lock (_harakaDb.GetLock(Setup.PublisherCs))
                    {
                        var pubs = _harakaDb.GetObjects<Publisher>(Setup.PublisherCs);
                        var publisher = new Publisher(message.IpAddress, message.Port, _harakaMqMessageBrokerConfiguration.PrimaryNumber, message.SenderClient);
                        pubs.Add(publisher);
                        _harakaDb.StoreObject(Setup.PublisherCs, pubs);
                        _publishers.Add(publisher);
                    }

                _queues.Find(x => x.GetTopicId() == message.Packet.Topic).AddEvent(new Event(message, EventType.TentativeMessage));
                lock (_messagesLock)
                {
                    _ownTentativeMessages.Add(message);
                }
            }
            else
            {
                _queues.Find(x => x.GetTopicId() == message.Packet.Topic).AddEvent(new Event(message, EventType.ComittedMessage));
                lock (_messagesLock)
                {
                    _committedMessages.Add(message);
                }
            }
        }

        public void QueueDeclareMessageReceived(MessageReceivedEventArgs message)
        {
            var createdSmartQueue = _smartQueueFactory.CreateSmartQueue(SmartQueueOnSubscribersHasBeenUpdated, message);
            if (createdSmartQueue != null)
            {
                _queues.Add(createdSmartQueue);
            }
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
            return _committedMessages.Where(x => x.Packet.Topic == smartQueue.GetTopicId() && x.Packet.GlobalSequenceNumber > smartQueue.GetLastGlobalSeqReceived()).ToList();
        }

        private void SmartQueueOnSubscribersHasBeenUpdated(object sender, List<Subscriber> subscribers)
        {
            foreach (var subscriber in subscribers)
                _subscribers.Find(x => x.ClientId == subscriber.ClientId).UpdateGobalSequenceNumber(subscriber.GlobalSequenceNumberLastReceived);
        }

        private void MergeCommittedMessagesAndPassThemToSmartQueues(AntiEntropyMessage antiEntropyMessage)
        {
            if (!antiEntropyMessage.Committed.Any()) return;

            _committedMessages.AddRange(antiEntropyMessage.Committed);
            foreach (var smartQueue in _queues)
                smartQueue.AddEvent(new Event(GetCommittedMessagesForSQ(smartQueue), EventType.ComittedMessages));
        }

        private void MergeTentativeMessagesAndPassThemToTheSmartQueues(AntiEntropyMessage antiEntropyMessage)
        {
            if (!antiEntropyMessage.Tentative.Any()) return;
            lock (_messagesLock)
            {
                _foreignTentativeMessages = _mergeProcedure.MergeMessages(antiEntropyMessage.Tentative, _foreignTentativeMessages);
            }

            foreach (var smartQueue in _queues)
            {
                smartQueue.AddEvent(new Event(_foreignTentativeMessages.Where(x => x.Packet.Topic == smartQueue.GetTopicId()).ToList(), EventType.TentativeMessages));
                smartQueue.AddEvent(new Event(antiEntropyMessage.Subscribers, EventType.AddSubscribers));
            }
        }
    }
}