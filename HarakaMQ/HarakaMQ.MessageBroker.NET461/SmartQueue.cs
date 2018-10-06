﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarakaMQ.MessageBroker.NET461.Events;
using HarakaMQ.MessageBroker.NET461.Interfaces;
using HarakaMQ.MessageBroker.NET461.Models;
using HarakaMQ.MessageBroker.NET461.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;

namespace HarakaMQ.MessageBroker.NET461
{
    public class SmartQueue : ISmartQueue, IDisposable
    {
        private readonly IPersistenceLayer _db;
        private readonly IJsonConfigurator _jsonConfigurator;

        /// <summary>
        ///     This is no a guranteed updated version of topic, always load from file before use
        /// </summary>
        private readonly Topic _topic;

        private readonly IUdpCommunication _udpComm;
        private Task _eventConsumerTask;
        private bool _stopConsuming;


        public SmartQueue(IUdpCommunication udpComm, IPersistenceLayer db, IJsonConfigurator jsonConfigurator, Topic topic)
        {
            _udpComm = udpComm;
            _topic = topic;
            _db = db;
            _jsonConfigurator = jsonConfigurator;
            _db.EventAdded += EventAdded;
            // Start Consuming messages if any
            if (_topic.Events.Count > 0)
                EventAdded(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            //Stop consumer thread
            _stopConsuming = true;
            _eventConsumerTask?.Wait();
            _eventConsumerTask?.Dispose();
        }

        public string GetTopicId()
        {
            return _topic.Name;
        }

        public void AddEvent(Event @event)
        {
            _db.AddEvent(_topic, @event);
        }

        public void UpdateLastGlobalSeqNoReceived(int seqNo)
        {
            _db.UpdatelastGlobalSeqReceived(_topic, seqNo);
        }

        public int GetLastGlobalSeqReceived()
        {
            return _topic.LastCommittedGlobalSeqNumberReceived;
        }

        public event EventHandler<List<Subscriber>> SubscribersHasBeenUpdated;

        private void EventAdded(object sender, EventArgs e)
        {
            if (_eventConsumerTask == null || _eventConsumerTask.IsCompleted)
                _eventConsumerTask = Task.Factory.StartNew(ConsumeMessage, TaskCreationOptions.LongRunning);
        }

        private void ConsumeMessage()
        {
            while (!_stopConsuming)
            {
                var @event = _db.GetNextEvent(_topic);
                if (@event == null)
                    return;

                switch (@event.EventType)
                {
                    case EventType.TentativeMessage:
                        PublishAndAddTentativeMessage(@event.Message);
                        break;
                    case EventType.AddSubscriber:
                        AddSubscriber(@event.Subscriber);
                        break;
                    case EventType.ComittedMessages:
                        PublishCommittedMessages(@event.Messages);
                        break;
                    case EventType.TentativeMessages:
                        PublishAndAddTentativeMessages(@event.Messages);
                        break;
                    case EventType.ComittedMessage:
                        PublishCommittedMessage(@event.Message);
                        break;
                    case EventType.AddSubscribers:
                        AddSubScribers(@event.Subscribers);
                        break;
                    default:
                        throw new ArgumentException(@event.EventType + " Not supported");
                }
                _db.EventHasBeenhandled(_topic, @event.Id);
            }
        }

        private void AddSubScribers(List<Subscriber> eventSubscribers)
        {
            _db.AddSubscribers(_topic, eventSubscribers);
        }

        private void PublishCommittedMessage(PublishPacketReceivedEventArgs message)
        {
            _topic.Subscribers = _db.GetSubScribers(_topic);
            foreach (var sub in _topic.Subscribers.Where(x => x.AttachedBroker == _jsonConfigurator.GetSettings().PrimaryNumber))
                _udpComm.SendPackage(message.Packet, sub.Ip, sub.Port);
        }

        //Todo: Enable to bulk messages to subscribers
        public void PublishCommittedMessages(IEnumerable<PublishPacketReceivedEventArgs> messages)
        {
            _topic.Subscribers = _db.GetSubScribers(_topic);
            foreach (var messageReceivedEventArgse in messages)
            foreach (var sub in _topic.Subscribers.Where(x => !x.ReceiveTentativeMessages && x.AttachedBroker == _jsonConfigurator.GetSettings().PrimaryNumber))
                if (messageReceivedEventArgse.Packet.GlobalSequenceNumber > sub.GlobalSequenceNumberLastReceived)
                {
                    _udpComm.SendPackage(messageReceivedEventArgse.Packet, sub.Ip, sub.Port);
                    sub.UpdateGobalSequenceNumber(messageReceivedEventArgse.Packet.GlobalSequenceNumber.Value);
                }

            _db.UpdateSubscribers(_topic);
            SubscribersHasBeenUpdated?.Invoke(this, _topic.Subscribers.Where(x => x.AttachedBroker == _jsonConfigurator.GetSettings().PrimaryNumber).ToList());
        }

        private void PublishAndAddTentativeMessages(IEnumerable<PublishPacketReceivedEventArgs> messages)
        {
            //Todo: store tentative messages
            _topic.Subscribers = _db.GetSubScribers(_topic);
            //PublishPackage tentative messages
            //Todo: Dont send messages to one that has already received it
            foreach (var messageReceivedEventArgse in messages)
            foreach (var sub in _topic.Subscribers.Where(x => x.ReceiveTentativeMessages && x.AttachedBroker == _jsonConfigurator.GetSettings().PrimaryNumber))
                _udpComm.SendPackage(messageReceivedEventArgse.Packet, sub.Ip, sub.Port);
        }

        private void PublishAndAddTentativeMessage(PublishPacketReceivedEventArgs message)
        {
            //Todo: store tentative messages
            _topic.Subscribers = _db.GetSubScribers(_topic);
            //Todo: Dont send messages to one that has already received it
            foreach (var sub in _topic.Subscribers.Where(x => x.ReceiveTentativeMessages && x.AttachedBroker == _jsonConfigurator.GetSettings().PrimaryNumber))
                _udpComm.SendPackage(message.Packet, sub.Ip, sub.Port);
        }

        public void AddSubscriber(Subscriber subscriber)
        {
            _db.AddSubscriber(_topic, subscriber);
            Console.WriteLine("Subscriber has been added: " + subscriber.ClientId);
        }
    }
}