using System.Collections.Generic;
using System.Linq;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Events;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.UnitTests.Utils;
using Shouldly;
using Xunit;

namespace HarakaMQ.UnitTests.HarakaMQ.MessageBroker
{
    public class PersistenceLayerTests : UnitTestExtension
    {
        public PersistenceLayerTests()
        {
            fileName = HarakaDb.CreatedFiles().First();
            _persistenceLayer = new PersistenceLayer(HarakaDb, fileName);
        }

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
    }
}