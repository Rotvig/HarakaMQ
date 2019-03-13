using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Events;
using HarakaMQ.MessageBroker.Models;
using Shouldly;
using Xunit;
using HarakaMQ.Shared;

namespace HarakaMQ.UnitTests.HarakaMQ.MessageBroker
{
    public class PersistenceLayerTests : IDisposable
    {
        public PersistenceLayerTests()
        {
            var serializer = new HarakaMessagePackSerializer();
            HarakaDb = new HarakaDb(serializer, "Test" + Guid.NewGuid());
            fileName = HarakaDb.CreatedFiles().First();
            _persistenceLayer = new PersistenceLayer(HarakaDb, fileName);
        }
        public IHarakaDb HarakaDb;

        private readonly PersistenceLayer _persistenceLayer;

        private readonly string fileName;

        [Fact]
        public void AddEvent()
        {
            var topic = new Topic("topic1");
            HarakaDb.StoreObject(fileName, new List<Topic> {topic});
            _persistenceLayer.AddEvent(topic, new Event(new Subscriber {Ip = "foo"}, EventType.AddSubscriber));

            var results = HarakaDb.TryGetObjects<Topic>(fileName).Find(x => x.Name == "topic1");
            results.Events.Count.ShouldBe(1);
            results.Events.First().EventType.ShouldBe(EventType.AddSubscriber);
            results.Events.First().Subscriber.Ip.ShouldBe("foo");
        }

        [Fact]
        public void EventHasBeenhandled()
        {
            var topic = new Topic("topic1");
            var @event = new Event(new Subscriber {Ip = "foo"}, EventType.AddSubscriber);
            topic.Events.Add(@event);
            HarakaDb.StoreObject(fileName, new List<Topic> {topic});

            _persistenceLayer.EventHasBeenhandled(topic, @event.Id);

            var results = HarakaDb.GetObjects<Topic>(fileName).Find(x => x.Name == "topic1");
            results.Events.Count.ShouldBe(0);
        }

        [Fact]
        public void GetNextEventAndHandleEvent()
        {
            var topic = new Topic("topic1");
            topic.Events.Add(new Event(new Subscriber {Ip = "foo1"}, EventType.AddSubscriber));
            topic.Events.Add(new Event(new Subscriber {Ip = "foo2"}, EventType.AddSubscriber));

            HarakaDb.StoreObject(fileName, new List<Topic> {topic});
            var item = _persistenceLayer.GetNextEvent(topic);
            item.EventType.ShouldBe(EventType.AddSubscriber);
            item.Subscriber.Ip.ShouldBe("foo1");
            _persistenceLayer.EventHasBeenhandled(topic, item.Id);

            var item2 = _persistenceLayer.GetNextEvent(topic);
            item2.EventType.ShouldBe(EventType.AddSubscriber);
            item2.Subscriber.Ip.ShouldBe("foo2");
        }

        public void Dispose()
        {
            foreach (var fileName in HarakaDb.CreatedFiles())
                File.Delete(fileName + ".db");
        }
    }
}