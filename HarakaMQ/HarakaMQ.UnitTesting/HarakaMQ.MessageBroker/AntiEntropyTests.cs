using System;
using System.Collections.Generic;
using FakeItEasy;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Models;
using Shouldly;
using Xunit;

namespace HarakaMQ.UnitTesting.HarakaMQ.MessageBroker
{
    public class AntiEntropyTests
    {
        public AntiEntropyTests()
        {
            _settings = A.Fake<IJsonConfigurator>();
            _harakaDb = A.Fake<IHarakaDb>();
            _mergeProcedure = A.Fake<IMergeProcedure>();

            ConfigureSettings();
            _antiEntropy = new AntiEntropy(_harakaDb, _mergeProcedure, _settings);
        }

        private readonly IJsonConfigurator _settings;
        private readonly IHarakaDb _harakaDb;
        private readonly IMergeProcedure _mergeProcedure;
        private AntiEntropy _antiEntropy;

        private static List<PublishPacketReceivedEventArgs> CreateTestMessages(int numOfMessages = 10, int offset = 0)
        {
            var messages = new List<PublishPacketReceivedEventArgs>();

            for (var i = 0; i < numOfMessages; i++)
            {
                messages.Add(new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet()
                    {
                        GlobalSequenceNumber = i
                    }
                });
            }
            return messages;
        }

        private void ConfigureSettings(bool primaryBroker = true)
        {
            A.CallTo(() => _settings.GetSettings()).Returns(new Settings
            {
                PrimaryNumber = primaryBroker ? 1 : 2,
                BrokerPort = primaryBroker ? 0 : 123,
                RunInClusterSetup = true,
                Brokers = new List<Broker>
                {
                    new Broker
                    {
                        PrimaryNumber = primaryBroker ? 2 : 1,
                        Port = primaryBroker ? 123 : 0,
                        Ipaddress = "foo"
                    }
                }
            });
        }

        [Fact]
        public void CanGetCommittedMessagesToSend()
        {
            var antiEntropyMessage = new AntiEntropyMessage
            {
                Committed = CreateTestMessages(5),
                Tentative = new List<PublishPacketReceivedEventArgs>()
            };
            _antiEntropy.AntiEntropyNonPrimaryMessageReceived(antiEntropyMessage);

            var byteOffSet = 5;
            var result = _antiEntropy.GetCommittedMessagesToSend(ref byteOffSet, offset: 0);

            result.Count.ShouldBe(5);
            result[0].Packet.GlobalSequenceNumber.ShouldBe(0);
            result[1].Packet.GlobalSequenceNumber.ShouldBe(1);
            result[2].Packet.GlobalSequenceNumber.ShouldBe(2);
            result[3].Packet.GlobalSequenceNumber.ShouldBe(3);
            result[4].Packet.GlobalSequenceNumber.ShouldBe(4);
        }

        [Fact]
        public void CanGetCommittedToSendOverMultipleRounds()
        {
            var antiEntropyMessage = new AntiEntropyMessage
            {
                Committed = CreateTestMessages(5),
                Tentative = new List<PublishPacketReceivedEventArgs>()
            };
            _antiEntropy.AntiEntropyNonPrimaryMessageReceived(antiEntropyMessage);
            var byteOffSet = 5;
            var resultRound1 = _antiEntropy.GetCommittedMessagesToSend(ref byteOffSet, offset: 0);

            resultRound1.Count.ShouldBe(5);
            resultRound1[0].Packet.GlobalSequenceNumber.ShouldBe(0);
            resultRound1[1].Packet.GlobalSequenceNumber.ShouldBe(1);
            resultRound1[2].Packet.GlobalSequenceNumber.ShouldBe(2);
            resultRound1[3].Packet.GlobalSequenceNumber.ShouldBe(3);
            resultRound1[4].Packet.GlobalSequenceNumber.ShouldBe(4);

            var resultRound2 = _antiEntropy.GetCommittedMessagesToSend(ref byteOffSet, offset: 5);
            resultRound2.Count.ShouldBe(5);
            resultRound2[0].Packet.GlobalSequenceNumber.ShouldBe(5);
            resultRound2[1].Packet.GlobalSequenceNumber.ShouldBe(6);
            resultRound2[2].Packet.GlobalSequenceNumber.ShouldBe(7);
            resultRound2[3].Packet.GlobalSequenceNumber.ShouldBe(8);
            resultRound2[4].Packet.GlobalSequenceNumber.ShouldBe(9);

            var resultRound3 = _antiEntropy.GetCommittedMessagesToSend(ref byteOffSet, offset: 10);
            resultRound3.Count.ShouldBe(0);
        }

        [Fact]
        public void CanGetTentativeMessagesToSendForNonPrimaryBrokers()
        {
            throw new NotImplementedException();

            //_antiEntropy.OwnTentativeMessages = CreateTestMessages(10, 0, 1);
            //_antiEntropy.ForeignTentativeMessages = CreateTestMessages(10, 10, 2);

            //var result = _antiEntropy.GetTentativeMessagesToSendForNonPrimaryBroker(5, currentAntiEntropyRound: 1);

            //result.Count.ShouldBe(5);
            //result.TrueForAll(x => x.Packet.AntiEntropyRound == 1 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(5);
            //_antiEntropy.ForeignTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(10);
        }

        [Fact]
        public void CanGetTentativeMessagesToSendForNonPrimaryBrokersOverMultipleRounds()
        {
            throw new NotImplementedException();

            //_antiEntropy.OwnTentativeMessages = CreateTestMessages(15, 0, 1);
            //_antiEntropy.ForeignTentativeMessages = CreateTestMessages(15, 15, 2);

            //var round1 = _antiEntropy.GetTentativeMessagesToSendForNonPrimaryBroker(5, currentAntiEntropyRound: 1);

            //round1.Count.ShouldBe(5);
            //round1.TrueForAll(x => x.Packet.AntiEntropyRound == 1 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(10);
            //_antiEntropy.ForeignTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(15);

            //var round2 = _antiEntropy.GetTentativeMessagesToSendForNonPrimaryBroker(15, currentAntiEntropyRound: 2);

            //round2.Count.ShouldBe(15);
            //round2.TrueForAll(x => x.Packet.AntiEntropyRound == 2 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(0);
            //_antiEntropy.ForeignTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(10);


            //var round3 = _antiEntropy.GetTentativeMessagesToSendForNonPrimaryBroker(10, currentAntiEntropyRound: 3);

            //round3.Count.ShouldBe(10);
            //round3.TrueForAll(x => x.Packet.AntiEntropyRound == 3 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(0);
            //_antiEntropy.ForeignTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(0);
        }

        [Fact]
        public void CanGetTentativeMessagesToSendForPrimaryBroker()
        {
            throw new NotImplementedException();

            //_antiEntropy.OwnTentativeMessages = CreateTestMessages();

            //var result = _antiEntropy.GetTentativeMessagesToSendForPrimaryBroker(5, currentAntiEntropyRound: 1);

            //result.Count.ShouldBe(5);
            //result.TrueForAll(x => x.Packet.AntiEntropyRound == 1 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => !x.Packet.AntiEntropyRound.HasValue).ShouldBe(5);
        }

        [Fact]
        public void CanGetTentativeMessagesToSendForPrimaryOverMultipleRounds()
        {
            throw new NotImplementedException();

            //_antiEntropy.OwnTentativeMessages = CreateTestMessages(30);

            //var round1 = _antiEntropy.GetTentativeMessagesToSendForPrimaryBroker(5, currentAntiEntropyRound: 1);

            //round1.Count.ShouldBe(5);
            //round1.TrueForAll(x => x.Packet.AntiEntropyRound == 1 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => x.Packet.AntiEntropyRound == null && x.Packet.Broker == 1).ShouldBe(25);

            //var round2 = _antiEntropy.GetTentativeMessagesToSendForPrimaryBroker(5, currentAntiEntropyRound: 2);

            //round2.Count.ShouldBe(5);
            //round2.TrueForAll(x => x.Packet.AntiEntropyRound == 2 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => x.Packet.AntiEntropyRound == null && x.Packet.Broker == 1).ShouldBe(20);

            //var round3 = _antiEntropy.GetTentativeMessagesToSendForPrimaryBroker(20, currentAntiEntropyRound: 3);

            //round3.Count.ShouldBe(20);
            //round3.TrueForAll(x => x.Packet.AntiEntropyRound == 3 && x.Packet.Broker == 1);
            //_antiEntropy.OwnTentativeMessages.Count(x => x.Packet.AntiEntropyRound == null && x.Packet.Broker == 1).ShouldBe(0);
        }

        [Fact]
        public void CanGetZeroCommittedMessagesToSendIfNoneIsRequsted()
        {
            throw new NotImplementedException();

            //_antiEntropy.CommittedMessages = CreateTestMessages(5);
            //var result = _antiEntropy.GetCommittedMessagesToSend(5, offset: 5);

            //result.Count.ShouldBe(0);
        }

        [Fact]
        public void DoesNotGetComittedMessagesIfTHeyAreRequestedBefore()
        {
            throw new NotImplementedException();
            //var messages = new List<MessageReceivedEventArgs>();

            //for (var i = 5; i < 10; i++)
            //{
            //    messages.Add(new MessageReceivedEventArgs
            //    {
            //        Packet = new Packet()
            //        {
            //            Broker = i % 2 == 0 ? 1 : 2,
            //            GlobalSequenceNumber = i
            //        }
            //    });
            //}

            //_antiEntropy.CommittedMessages = messages;
            //var result = _antiEntropy.GetCommittedMessagesToSend(5, offset: 0);

            //result.Count.ShouldBe(0);
        }
    }
}