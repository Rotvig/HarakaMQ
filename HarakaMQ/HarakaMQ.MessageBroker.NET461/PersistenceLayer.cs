using System;
using System.Collections.Generic;
using System.Linq;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker.NET461.Events;
using HarakaMQ.MessageBroker.NET461.Interfaces;
using HarakaMQ.MessageBroker.NET461.Models;

namespace HarakaMQ.MessageBroker.NET461
{
    public class PersistenceLayer : IPersistenceLayer
    {
        private readonly IHarakaDb _db;
        private readonly string _fileName;


        public PersistenceLayer(IHarakaDb db, string fileName)
        {
            _db = db;
            _fileName = fileName;
        }

        public event EventHandler EventAdded;

        public void AddEvent(Topic topic, Event @event)
        {
            lock (_db.GetLock(_fileName))
            {
                var topics = _db.GetObjects<Topic>(_fileName);
                topics.Find(x => x.Id == topic.Id).Events.Add(@event);
                _db.StoreObject(_fileName, topics);
            }
            EventAddedToQueue();
        }

        public Event GetNextEvent(Topic topic)
        {
            return _db.TryGetObjects<Topic>(_fileName).Find(x => x.Id == topic.Id).Events.FirstOrDefault();
        }

        public void EventHasBeenhandled(Topic topic, Guid eventId)
        {
            lock (_db.GetLock(_fileName))
            {
                var topics = _db.GetObjects<Topic>(_fileName);
                var condeemnedEvent = topics.Find(x => x.Id == topic.Id).Events.Find(x => x.Id == eventId);
                topics.Find(x => x.Id == topic.Id).Events.Remove(condeemnedEvent);
                _db.StoreObject(_fileName, topics);
            }
        }

        public void AddSubscriber(Topic topic, Subscriber subscriber)
        {
            lock (_db.GetLock(_fileName))
            {
                var topics = _db.GetObjects<Topic>(_fileName);
                var topicTochange = topics.Find(x => x.Id == topic.Id);
                if (!topicTochange.Subscribers.Exists(x => x.ClientId == subscriber.ClientId))
                {
                    topicTochange.Subscribers.Add(subscriber);
                    _db.StoreObject(_fileName, topics);
                }
            }
        }

        public void UpdateSubscribers(Topic topic)
        {
            lock (_db.GetLock(_fileName))
            {
                var topics = _db.GetObjects<Topic>(_fileName);
                var topicToChange = topics.Find(x => x.Id == topic.Id);

                foreach (var topicSubscriber in topic.Subscribers)
                    topicToChange.Subscribers.Find(x => x.ClientId == topicSubscriber.ClientId)
                            .GlobalSequenceNumberLastReceived =
                        topicSubscriber.GlobalSequenceNumberLastReceived;
                _db.StoreObject(_fileName, topics);
            }
        }

        public void UpdatelastGlobalSeqReceived(Topic topic, int seqNo)
        {
            lock (_db.GetLock(_fileName))
            {
                var topics = _db.GetObjects<Topic>(_fileName);
                var topicToChange = topics.Find(x => x.Id == topic.Id);
                topicToChange.LastCommittedGlobalSeqNumberReceived = seqNo;
                _db.StoreObject(_fileName, topics);
            }
        }

        public List<Subscriber> GetSubScribers(Topic topic)
        {
            return _db.TryGetObjects<Topic>(_fileName).Find(x => x.Id == topic.Id).Subscribers;
        }

        public void AddSubscribers(Topic topic, List<Subscriber> eventSubscribers)
        {
            lock (_db.GetLock(_fileName))
            {
                var topics = _db.GetObjects<Topic>(_fileName);
                var editableTopic = topics.Find(x => x.Id == topic.Id);
                editableTopic.Subscribers.AddRange(eventSubscribers);
                var uniqSubscribers =
                    editableTopic.Subscribers.GroupBy(x => x.ClientId).Select(y => y.FirstOrDefault()).ToList();
                editableTopic.Subscribers = uniqSubscribers;
                _db.StoreObject(_fileName, topics);
            }
        }

        protected virtual void EventAddedToQueue()
        {
            EventAdded?.Invoke(this, EventArgs.Empty);
        }
    }
}