using System;
using System.Collections.Generic;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;

namespace HarakaMQ.MessageBroker.Models
{
    internal class SmartQueueFactory : ISmartQueueFactory
    {
        private readonly IHarakaDb _harakaDb;
        private readonly IUdpCommunication _udpCommunication;
        private readonly IPersistenceLayer _persistenceLayer;
        private readonly HarakaMqMessageBrokerConfiguration _harakaMqMessageBrokerConfiguration;

        public SmartQueueFactory(IHarakaDb harakaDb, IUdpCommunication udpCommunication, IPersistenceLayer persistenceLayer, HarakaMqMessageBrokerConfiguration harakaMqMessageBrokerConfiguration)
        {
            _harakaDb = harakaDb;
            _udpCommunication = udpCommunication;
            _persistenceLayer = persistenceLayer;
            _harakaMqMessageBrokerConfiguration = harakaMqMessageBrokerConfiguration;
        }
        
        public List<ISmartQueue> InitializeSmartQueues(EventHandler<List<Subscriber>> smartQueueOnSubscribersHasBeenUpdated)
        {
            var smartQueues = new List<ISmartQueue>();
            //Initialize all existing queues on startup
            foreach (var topic in _harakaDb.TryGetObjects<Topic>("Topics"))
            {
                var smartQueue = new SmartQueue(_udpCommunication, _persistenceLayer, _harakaMqMessageBrokerConfiguration, topic);
                smartQueue.SubscribersHasBeenUpdated += smartQueueOnSubscribersHasBeenUpdated;
                smartQueues.Add(smartQueue);
            }

            return smartQueues;
        }

        public ISmartQueue CreateSmartQueue(EventHandler<List<Subscriber>> smartQueueOnSubscribersHasBeenUpdated, MessageReceivedEventArgs message)
        {
            var topics = _harakaDb.TryGetObjects<Topic>("Topics");
            var topic = topics.Find(x => x.Name == message.AdministrationMessage.Topic);

            if (topic != null) return null;

            lock (_harakaDb.GetLock("Topics"))
            {
                topics = _harakaDb.GetObjects<Topic>("Topics");
                topic = new Topic(message.AdministrationMessage.Topic);
                topics.Add(topic);
                _harakaDb.StoreObject("Topics", topics);
            }

            var smartQueue = new SmartQueue(Setup.container.GetInstance<IUdpCommunication>(), Setup.container.GetInstance<IPersistenceLayer>(), _harakaMqMessageBrokerConfiguration, topic);
            smartQueue.SubscribersHasBeenUpdated += smartQueueOnSubscribersHasBeenUpdated;
            return smartQueue;
        }
    }
}