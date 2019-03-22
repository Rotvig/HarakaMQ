﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.MessageBroker
{
    public class PingPong : IGossip
    {
        private readonly IAntiEntropy _antiEntropy;
        private readonly IHarakaMQMessageBrokerConfiguration _harakaMqMessageBrokerConfiguration;
        private readonly List<MessageBrokerInformation> _messageBrokers = new List<MessageBrokerInformation>();
        private readonly ITimeSyncProtocol _timeSyncProtocol;
        private readonly ISerializer _serializer;
        private readonly int _latencyInMs;
        private readonly ISchedular _schedular;
        private readonly IUdpCommunication _udpCommunication;
        private int _currentAntiEntropyRound;
        private int _globalSequenceNumber;
        private bool _isPrimary;
        private int _lastAntiEntropyCommit;
        private bool _stopGossip;

        public PingPong(IUdpCommunication udpCommunication, ISchedular schedular, IAntiEntropy antiEntropy, IHarakaMQMessageBrokerConfiguration harakaMqMessageBrokerConfiguration, ITimeSyncProtocol timesyncProtocol, ISerializer serializer)
        {
            _schedular = schedular;
            _antiEntropy = antiEntropy;
            _harakaMqMessageBrokerConfiguration = harakaMqMessageBrokerConfiguration;
            _timeSyncProtocol = timesyncProtocol;
            _serializer = serializer;
            _udpCommunication = udpCommunication;
            _latencyInMs = harakaMqMessageBrokerConfiguration.AntiEntropyMilliseonds / 10; //Todo: find real latency

            if (harakaMqMessageBrokerConfiguration.MessageBrokers != null)
            {
                foreach (var broker in harakaMqMessageBrokerConfiguration?.MessageBrokers)
                    _messageBrokers.Add(new MessageBrokerInformation
                    {
                        Active = true,
                        PrimaryNumber = broker.PrimaryNumber,
                        Host = broker.Host
                    });
            }
        }

        public void SubScribeMessageReceived(MessageReceivedEventArgs message)
        {
            _antiEntropy.SubScribeMessageReceived(message);
        }

        public void PublishMessageReceived(PublishPacketReceivedEventArgs publishPacketReceivedEventArgs)
        {
            publishPacketReceivedEventArgs.Packet.ReceivedAtBroker = _timeSyncProtocol.GetTime();
            _antiEntropy.PublishMessageReceived(publishPacketReceivedEventArgs);
            Debug.WriteLine("Messages Published to Broker - Number: " + publishPacketReceivedEventArgs.Packet.SeqNo + " Time: " + publishPacketReceivedEventArgs.Packet.ReceivedAtBroker);
        }

        public void QueueDeclareMessageReceived(MessageReceivedEventArgs message)
        {
            _antiEntropy.QueueDeclareMessageReceived(message);
        }

        public void AntiEntropyMessageReceived(MessageReceivedEventArgs messageReceivedEventArgs)
        {
            var antiEntropyMessage = _serializer.Deserialize<AntiEntropyMessage>(messageReceivedEventArgs.AdministrationMessage.Data);
            var senderBroker = _messageBrokers.Find(x => x.Host.IPAddress == messageReceivedEventArgs.IpAddress && x.Host.Port == messageReceivedEventArgs.Port);
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
                if (_messageBrokers.All(x => x.Active && x.CurrentAntiEntropyRound >= _lastAntiEntropyCommit + 3))
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

            if (_harakaMqMessageBrokerConfiguration.PrimaryNumber != 1)
                return;
            _timeSyncProtocol.StartTimeSync();
            _isPrimary = true;

            if (_harakaMqMessageBrokerConfiguration.RunInClusterSetup)
                StartGossiping();
        }

        public void StopGossip()
        {
            _stopGossip = true;
        }

        private void StartGossiping()
        {
            _schedular.ScheduleTask(_harakaMqMessageBrokerConfiguration.AntiEntropyMilliseonds, Guid.NewGuid(), () =>
            {
                if (_stopGossip) return;
                _currentAntiEntropyRound++;
                SendAntiEntropyMessage();
                StartGossiping();
            });
        }

        private void SendAntiEntropyMessage()
        {
            foreach (var broker in _messageBrokers.Where(x => x.Active))
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

                var administrationMessage = new AdministrationMessage(MessageType.AntiEntropy, _serializer.Serialize(new AntiEntropyMessage
                {
                    PrimaryNumber = _harakaMqMessageBrokerConfiguration.PrimaryNumber,
                    Tentative = tentativeMessagesToSend,
                    Committed = comittedMessagesToSend,
                    Publishers = _antiEntropy.GetPublishers(),
                    Subscribers = _antiEntropy.GetSubscribers(),
                    AntiEntropyRound = _currentAntiEntropyRound,
                    Primary = _isPrimary ? _harakaMqMessageBrokerConfiguration.PrimaryNumber : 0,
                    AntiEntropyGarbageCollect = _currentAntiEntropyRound % 3 == 0 // GC every third round
                }));
                _udpCommunication.SendAdministrationMessage(administrationMessage, broker.Host);
                Console.WriteLine("Time: " + _timeSyncProtocol.GetTime());
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
            Console.WriteLine("Time: " + _timeSyncProtocol.GetTime());

            _currentAntiEntropyRound = antiEntropyMessage.AntiEntropyRound;
            var primaryBroker = _messageBrokers.Find(x => x.PrimaryNumber == antiEntropyMessage.PrimaryNumber);

            _udpCommunication.SendAdministrationMessage(new AdministrationMessage(MessageType.AntiEntropy, _serializer.Serialize(new AntiEntropyMessage
            {
                PrimaryNumber = _harakaMqMessageBrokerConfiguration.PrimaryNumber,
                Tentative = _antiEntropy.GetTentativeMessagesToSendForPrimaryBroker(_currentAntiEntropyRound),
                Publishers = _antiEntropy.GetPublishers(),
                Subscribers = _antiEntropy.GetSubscribers(),
                AntiEntropyRound = _currentAntiEntropyRound,
                Primary = _isPrimary ? _harakaMqMessageBrokerConfiguration.PrimaryNumber : 0
            })), primaryBroker.Host);
            //StartWaitForPrimaryBroker(primaryBroker);
        }

        private void StartWaitForPrimaryBroker(MessageBrokerInformation primaryMessageBroker, int extraTime = 0)
        {
            _schedular.CancelTask(primaryMessageBroker.AntiEntropyRoundScheduledTaskIdResponse);
            primaryMessageBroker.AntiEntropyRoundScheduledTaskIdResponse = Guid.NewGuid();
            _schedular.ScheduleTask(_harakaMqMessageBrokerConfiguration.AntiEntropyMilliseonds + _latencyInMs + extraTime,
                primaryMessageBroker.AntiEntropyRoundScheduledTaskIdResponse, () =>
                {
                    //Todo: BroadCast Primary broker down to sub/pub/brokers - Round 1
                    //Start a new schedular which waites for the next primary to broadcast - Round 2
                    //No one has identified themself then it takes over for itself
                });
        }
    }
}