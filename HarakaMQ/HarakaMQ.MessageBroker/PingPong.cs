using System;
using System.Collections.Generic;
using System.Linq;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using MessagePack;

namespace HarakaMQ.MessageBroker
{
    public class PingPong : IGossip
    {
        private readonly IAntiEntropy _antiEntropy;
        private readonly List<BrokerInformation> _brokers = new List<BrokerInformation>();
        private readonly IClock _clock;
        private readonly IJsonConfigurator _jsonConfigurator;
        private readonly int _latencyInMs;
        private readonly ISchedular _schedular;
        private readonly IUdpCommunication _udpCommunication;
        private volatile int _currentAntiEntropyRound;
        private int _globalSequenceNumber;
        private bool _isPrimary;
        private volatile int _lastAntiEntropyCommit;
        private volatile bool _stopGossip;

        public PingPong(IUdpCommunication udpCommunication, ISchedular schedular, IAntiEntropy antiEntropy, IJsonConfigurator jsonConfigurator, IClock clock)
        {
            _schedular = schedular;
            _antiEntropy = antiEntropy;
            _jsonConfigurator = jsonConfigurator;
            _clock = clock;
            _udpCommunication = udpCommunication;

            _latencyInMs = _jsonConfigurator.GetSettings().AntiEntropyMilliseonds / 10; //Todo: find real latency

            foreach (var broker in _jsonConfigurator.GetSettings().Brokers)
                _brokers.Add(new BrokerInformation
                {
                    Active = true,
                    PrimaryNumber = broker.PrimaryNumber,
                    Port = broker.Port,
                    Ipadress = broker.Ipadress
                });
        }

        public void SubScribeMessageReceived(MessageReceivedEventArgs message)
        {
            _antiEntropy.SubScribeMessageReceived(message);
        }

        public void PublishMessageReceived(PublishPacketReceivedEventArgs publishPacketReceivedEventArgs)
        {
            publishPacketReceivedEventArgs.Packet.ReceivedAtBroker = _clock.ElapsedTimeSpan();
            _antiEntropy.PublishMessageReceived(publishPacketReceivedEventArgs);
            //Debug.WriteLine("Messages Published to Broker - Number: " + message.AdministrationMessage.SeqNo + " Time: " + message.AdministrationMessage.ReceivedAtBroker);
        }

        public void QueueDeclareMessageReceived(MessageReceivedEventArgs message)
        {
            _antiEntropy.QueueDeclareMessageReceived(message);
        }

        public void AntiEntropyMessageReceived(MessageReceivedEventArgs messageReceivedEventArgs)
        {
            var antiEntropyMessage = MessagePackSerializer.Deserialize<AntiEntropyMessage>(messageReceivedEventArgs.AdministrationMessage.Data);
            var senderBroker = _brokers.Find(x => x.Ipadress == messageReceivedEventArgs.IpAddress && x.Port == messageReceivedEventArgs.Port);
            senderBroker.CurrentAntiEntropyRound = antiEntropyMessage.AntiEntropyRound;
            //Todo: remove this
            Console.WriteLine("AntiEntropyMessage " + antiEntropyMessage.AntiEntropyRound);
            Console.WriteLine("Tentative Messages: " + antiEntropyMessage.Tentative.Count + " Received from: " + antiEntropyMessage.PrimaryNumber);

            //if (!senderBroker.Active)
            //{
            //    //Todo: Broadcast back to it that it was DCed / who is the primary now
            //    //Make sure the broker gets all the information to get insync
            //    return;
            //}

            if (!_isPrimary)
            {
                Console.WriteLine("Committed Messages: " + antiEntropyMessage.Committed.Count + " Received from: " + antiEntropyMessage.PrimaryNumber);

                AntiEntropyResponse(antiEntropyMessage);
                _antiEntropy.AntiEntropyNonPrimaryMessageReceived(antiEntropyMessage);
            }
            else
            {
                //Cancel the scheduled task from the expected non primary broker
                _schedular.CancelTask(senderBroker.AntiEntropyRoundScheduledTaskIdAnswer);
                //Commit messages after they have survived 3 rounds
                if (_brokers.All(x => x.Active && x.CurrentAntiEntropyRound >= _lastAntiEntropyCommit + 3))
                {
                    _antiEntropy.AntiEntropyAddTentativeMessages(antiEntropyMessage, ref _currentAntiEntropyRound, ref _globalSequenceNumber);
                    _antiEntropy.AntiEntropyCommitStableMessages(ref _lastAntiEntropyCommit, ref _globalSequenceNumber);
                }
                else
                {
                    _antiEntropy.AntiEntropyAddTentativeMessages(antiEntropyMessage, ref _currentAntiEntropyRound, ref _globalSequenceNumber);
                }
            }

            //Todo find af better solution than antiEntropyMessage.Committed.Any()
            if (antiEntropyMessage.AntiEntropyGarbageCollect && antiEntropyMessage.Committed.Any())
                _antiEntropy.GarbageCollectMessages(antiEntropyMessage.Committed.Last().Packet.GlobalSequenceNumber.Value);
        }

        public void StartGossip()
        {
            _stopGossip = false;

            if (_jsonConfigurator.GetSettings().PrimaryNumber != 1)
                return;
            _clock.StartTimeSync(_brokers);
            _isPrimary = true;

            if (_jsonConfigurator.GetSettings().RunInClusterSetup)
                StartGossiping();
        }

        public void StopGossip()
        {
            _stopGossip = true;
        }

        public void ClockSyncMessageReceived(MessageReceivedEventArgs messageReceivedEventArgs)
        {
            _clock.ClockSyncMessageReceived(messageReceivedEventArgs);
        }

        private void StartGossiping()
        {
            _schedular.ScheduleTask(_jsonConfigurator.GetSettings().AntiEntropyMilliseonds, Guid.NewGuid(), () =>
            {
                if (_stopGossip) return;
                _currentAntiEntropyRound++;
                SendAntiEntropyMessage();
                StartGossiping();
            });
        }

        private void SendAntiEntropyMessage()
        {
            foreach (var broker in _brokers.Where(x => x.Active))
            {
                var numberOfBytesUsed = 0;
                var tentativeMessagesToSend = _antiEntropy.GetTentativeMessagesToSendForNonPrimaryBroker(ref numberOfBytesUsed, _currentAntiEntropyRound);

                // + 1 because LastCommittedSeqNumberReceived represent the last received, and that number of message should not be resend
                var comittedMessagesToSend = _antiEntropy.GetCommittedMessagesToSend(ref numberOfBytesUsed, broker.LastCommittedSeqNumberReceived + 1);
                if (comittedMessagesToSend.Any())
                    broker.LastCommittedSeqNumberReceived = comittedMessagesToSend.Last().Packet.GlobalSequenceNumber.Value;

                broker.AntiEntropyRoundScheduledTaskIdAnswer = Guid.NewGuid();
                Console.WriteLine("Committed Messages " + comittedMessagesToSend.Count);
                Console.WriteLine("Tentative Messages " + tentativeMessagesToSend.Count);

                var administrationMessage = new AdministrationMessage(MessageType.AntiEntropy, MessagePackSerializer.Serialize(new AntiEntropyMessage
                {
                    PrimaryNumber = _jsonConfigurator.GetSettings().PrimaryNumber,
                    Tentative = tentativeMessagesToSend,
                    Committed = comittedMessagesToSend,
                    Publishers = _antiEntropy.GetPublishers(),
                    Subscribers = _antiEntropy.GetSubscribers(),
                    AntiEntropyRound = _currentAntiEntropyRound,
                    Primary = _isPrimary ? _jsonConfigurator.GetSettings().PrimaryNumber : 0,
                    AntiEntropyGarbageCollect = _currentAntiEntropyRound % 3 == 0 // GC every third round
                }));
                _udpCommunication.SendAdministrationMessage(administrationMessage, broker.Ipadress, broker.Port);
                Console.WriteLine("Time: " + _clock.ElapsedTimeSpan());
                //TODO: activate this later on
                //_schedular.ScheduleTask(Setup.Settings.AntiEntropyMilliseonds, broker.AntiEntropyRoundScheduledTaskIdAnswer, () =>
                //{
                //    broker.DeactivateBroker();
                //    //Todo: Broadcast to subs/pubs/brokers that a broker is down
                //});
            }
        }

        private void AntiEntropyResponse(AntiEntropyMessage antiEntropyMessage)
        {
            Console.WriteLine("Time: " + _clock.ElapsedTimeSpan());

            _currentAntiEntropyRound = antiEntropyMessage.AntiEntropyRound;
            var primaryBroker = _brokers.Find(x => x.PrimaryNumber == antiEntropyMessage.PrimaryNumber);

            var numberOfBytesUsed = 0;
            _udpCommunication.SendAdministrationMessage(new AdministrationMessage(MessageType.AntiEntropy, MessagePackSerializer.Serialize(new AntiEntropyMessage
            {
                PrimaryNumber = _jsonConfigurator.GetSettings().PrimaryNumber,
                Tentative = _antiEntropy.GetTentativeMessagesToSendForPrimaryBroker(ref numberOfBytesUsed, _currentAntiEntropyRound),
                Publishers = _antiEntropy.GetPublishers(),
                Subscribers = _antiEntropy.GetSubscribers(),
                AntiEntropyRound = _currentAntiEntropyRound,
                Primary = _isPrimary ? _jsonConfigurator.GetSettings().PrimaryNumber : 0
            })), primaryBroker.Ipadress, primaryBroker.Port);
            //StartWaitForPrimaryBroker(primaryBroker);
        }

        private void StartWaitForPrimaryBroker(BrokerInformation primaryBroker, int extraTime = 0)
        {
            _schedular.CancelTask(primaryBroker.AntiEntropyRoundScheduledTaskIdResponse);
            primaryBroker.AntiEntropyRoundScheduledTaskIdResponse = Guid.NewGuid();
            _schedular.ScheduleTask(_jsonConfigurator.GetSettings().AntiEntropyMilliseonds + _latencyInMs + extraTime,
                primaryBroker.AntiEntropyRoundScheduledTaskIdResponse, () =>
                {
                    //Todo: BroadCast Primary broker down to sub/pub/brokers - Round 1
                    //Start a new schedular which waites for the next primary to broadcast - Round 2
                    //No one has identified themself then it takes over for itself
                });
        }
    }
}