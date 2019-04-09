using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture.Xunit2;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Events;
using HarakaMQ.MessageBroker.Models;
using Shouldly;
using Xunit;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication.Models;

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

        [Theory, AutoData]
        public void AddEvent([Frozen] Host host, Topic topic, Event @event)
        {
            HarakaDb.StoreObject(fileName, new List<Topic> {topic});
            _persistenceLayer.AddEvent(topic, @event);

            var results = HarakaDb.TryGetObjects<Topic>(fileName).Find(x => x.Name == topic.Name);
            results.Events.Count.ShouldBe(4);
            results.Events.Last().EventType.ShouldBe(@event.EventType);
            results.Events.Last().Subscriber.Host.IPAddress.ShouldBe(host.IPAddress);
        }

        [Theory, AutoData]
        public void EventHasBeenhandled(Topic topic, Event @event)
        {
            topic.Events.Add(@event);
            HarakaDb.StoreObject(fileName, new List<Topic> {topic});

            _persistenceLayer.EventHasBeenhandled(topic, @event.Id);

            var results = HarakaDb.GetObjects<Topic>(fileName).Find(x => x.Name == topic.Name);
            results.Events.Count.ShouldBe(3);
        }

        [Theory, AutoData]
        public void GetNextEventAndHandleEvent(Topic topic, Event @event1, Event @event2)
        {
            topic.Events.Clear();
            topic.Events.Add(@event1);
            topic.Events.Add(@event2);

            HarakaDb.StoreObject(fileName, new List<Topic> {topic});
            var item = _persistenceLayer.GetNextEvent(topic);
            item.EventType.ShouldBe(@event1.EventType);
            item.Subscriber.Host.IPAddress.ShouldBe(@event1.Subscriber.Host.IPAddress);
            _persistenceLayer.EventHasBeenhandled(topic, item.Id);

            var item2 = _persistenceLayer.GetNextEvent(topic);
            item2.EventType.ShouldBe(@event2.EventType);
            item2.Subscriber.Host.IPAddress.ShouldBe(@event2.Subscriber.Host.IPAddress);
        }

        public void Dispose()
        {
            foreach (var fileName in HarakaDb.CreatedFiles())
                File.Delete(fileName + ".db");
        }
    }
}