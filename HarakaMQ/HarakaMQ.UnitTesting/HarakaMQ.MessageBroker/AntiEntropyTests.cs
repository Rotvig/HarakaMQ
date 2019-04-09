using System;
using System.Collections.Generic;
using AutoFixture.Xunit2;
using FakeItEasy;
using HarakaMQ.DB;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using HarakaMQ.UnitTests.Utils;
using Shouldly;
using Xunit;
using Setup = HarakaMQ.MessageBroker.Utils.Setup;

namespace HarakaMQ.UnitTests.HarakaMQ.MessageBroker
{
    public class AntiEntropyTests
    {
        [Theory, AutoFakeItEasyData]
        public void CanGetCommittedMessagesToSend(
            [Frozen] IHarakaDb harakaDb,
            [Frozen] IMergeProcedure mergeProcedure,
            [Frozen] ISmartQueueFactory smartQueueFactory,
            AntiEntropy sut)
        {
            var antiEntropyMessage = new AntiEntropyMessage
            {
                Committed = CreateTestMessages(5, 5),
                Tentative = new List<PublishPacketReceivedEventArgs>()
            };
            sut.AntiEntropyNonPrimaryMessageReceived(antiEntropyMessage);

            var byteOffSet = 0;
            var result = sut.GetCommittedMessagesToSend(ref byteOffSet, globalSequenceNumberOffset: 0);

            result.Count.ShouldBe(5);
            result[0].Packet.GlobalSequenceNumber.ShouldBe(0);
            result[1].Packet.GlobalSequenceNumber.ShouldBe(1);
            result[2].Packet.GlobalSequenceNumber.ShouldBe(2);
            result[3].Packet.GlobalSequenceNumber.ShouldBe(3);
            result[4].Packet.GlobalSequenceNumber.ShouldBe(4);
        }

        [Theory, AutoFakeItEasyData]
        public void CanGetCommittedToSendOverMultipleRounds(
            [Frozen] IHarakaDb harakaDb,
            [Frozen] IMergeProcedure mergeProcedure,
            [Frozen] ISmartQueueFactory smartQueueFactory,
            AntiEntropy sut)
        {
            var antiEntropyMessage = new AntiEntropyMessage
            {
                Committed = CreateTestMessages(10, 5),
                Tentative = new List<PublishPacketReceivedEventArgs>()
            };
            sut.AntiEntropyNonPrimaryMessageReceived(antiEntropyMessage);
            var byteOffSet = 0;
            var resultRound1 = sut.GetCommittedMessagesToSend(ref byteOffSet, globalSequenceNumberOffset: 0);

            resultRound1.Count.ShouldBe(5);
            resultRound1[0].Packet.GlobalSequenceNumber.ShouldBe(0);
            resultRound1[1].Packet.GlobalSequenceNumber.ShouldBe(1);
            resultRound1[2].Packet.GlobalSequenceNumber.ShouldBe(2);
            resultRound1[3].Packet.GlobalSequenceNumber.ShouldBe(3);
            resultRound1[4].Packet.GlobalSequenceNumber.ShouldBe(4);

            byteOffSet = 0;
            var resultRound2 = sut.GetCommittedMessagesToSend(ref byteOffSet, globalSequenceNumberOffset: 5);
            resultRound2.Count.ShouldBe(5);
            resultRound2[0].Packet.GlobalSequenceNumber.ShouldBe(5);
            resultRound2[1].Packet.GlobalSequenceNumber.ShouldBe(6);
            resultRound2[2].Packet.GlobalSequenceNumber.ShouldBe(7);
            resultRound2[3].Packet.GlobalSequenceNumber.ShouldBe(8);
            resultRound2[4].Packet.GlobalSequenceNumber.ShouldBe(9);

            var resultRound3 = sut.GetCommittedMessagesToSend(ref byteOffSet, globalSequenceNumberOffset: 10);
            resultRound3.Count.ShouldBe(0);
        }

        [Theory, AutoFakeItEasyData]
        public void CanGetTentativeMessagesToSendForNonPrimaryBrokers(
            [Frozen] IHarakaDb harakaDb,
            [Frozen] IMergeProcedure mergeProcedure,
            [Frozen] ISmartQueueFactory smartQueueFactory,
            AntiEntropy sut)
        {           
            var antiEntropyMessage = new AntiEntropyMessage
            {
                Committed = new List<PublishPacketReceivedEventArgs>(),
                Tentative = CreateTestMessages(5, 5)
            };
            A.CallTo(() => mergeProcedure.MergeMessages(A<IEnumerable<PublishPacketReceivedEventArgs>>.Ignored, A<IEnumerable<PublishPacketReceivedEventArgs>>.Ignored)).Returns(antiEntropyMessage.Tentative);
            sut.AntiEntropyNonPrimaryMessageReceived(antiEntropyMessage);

            var byteOffSet = 0;
            var result = sut.GetTentativeMessagesToSendForNonPrimaryBroker(ref byteOffSet, currentAntiEntropyRound: 1);

            result.Count.ShouldBe(5);
            result.TrueForAll(x => x.Packet.AntiEntropyRound == 1);
        }

        [Theory, AutoFakeItEasyData]
        public void CanGetTentativeMessagesToSendForNonPrimaryBrokersOverMultipleRounds(
            [Frozen] IHarakaDb harakaDb,
            [Frozen] IMergeProcedure mergeProcedure,
            [Frozen] ISmartQueueFactory smartQueueFactory,
            AntiEntropy sut)
        {
            var antiEntropyMessage = new AntiEntropyMessage
            {
                Committed = new List<PublishPacketReceivedEventArgs>(),
                Tentative = CreateTestMessages(15, 5)
            };
            A.CallTo(() => mergeProcedure.MergeMessages(A<IEnumerable<PublishPacketReceivedEventArgs>>.Ignored, A<IEnumerable<PublishPacketReceivedEventArgs>>.Ignored)).Returns(antiEntropyMessage.Tentative);
            sut.AntiEntropyNonPrimaryMessageReceived(antiEntropyMessage);
            
            var byteOffSet = 0;
            
            var round1 = sut.GetTentativeMessagesToSendForNonPrimaryBroker(ref byteOffSet, currentAntiEntropyRound: 1);
            round1.Count.ShouldBe(5);
            round1.TrueForAll(x => x.Packet.AntiEntropyRound == 1);
            
            byteOffSet = 0;
            var round2 = sut.GetTentativeMessagesToSendForNonPrimaryBroker(ref byteOffSet, currentAntiEntropyRound: 2);
            round2.Count.ShouldBe(5);
            round2.TrueForAll(x => x.Packet.AntiEntropyRound == 2);
            
            byteOffSet = 0;
            var round3 = sut.GetTentativeMessagesToSendForNonPrimaryBroker(ref byteOffSet, currentAntiEntropyRound: 3);
            round3.Count.ShouldBe(5);
            round3.TrueForAll(x => x.Packet.AntiEntropyRound == 3);
            
            byteOffSet = 0;
            var round4 = sut.GetTentativeMessagesToSendForNonPrimaryBroker(ref byteOffSet, currentAntiEntropyRound: 4);
            round4.Count.ShouldBe(0);
        }

        [Theory, AutoFakeItEasyData]
        public void CanGetTentativeMessagesToSendForPrimaryBroker(
            [Frozen] IHarakaDb harakaDb,
            [Frozen] IMergeProcedure mergeProcedure,
            [Frozen] IHarakaMQMessageBrokerConfiguration harakaMqMessageBrokerConfiguration,
            [Frozen] ISmartQueueFactory smartQueueFactory,
            AntiEntropy sut,
            SmartQueue smartQueue)
        {
            A.CallTo(() => smartQueueFactory.InitializeSmartQueues(A<EventHandler<List<Subscriber>>>.Ignored)).Returns(new List<ISmartQueue>{smartQueue});
            sut.Initialize();
            ConfigureSettings(harakaMqMessageBrokerConfiguration);
            
            foreach (var message in CreateTestMessages(5,5, smartQueue.GetTopicId()))
            {
                sut.PublishMessageReceived(message);
            }
            
            var round1 = sut.GetTentativeMessagesToSendForPrimaryBroker(currentAntiEntropyRound: 1);

            round1.Count.ShouldBe(5);
            round1.TrueForAll(x => x.Packet.AntiEntropyRound == 1);
            
            sut.GetTentativeMessagesToSendForPrimaryBroker(currentAntiEntropyRound: 2).Count.ShouldBe(0);
        }

        [Theory, AutoFakeItEasyData]
        public void CanGetTentativeMessagesToSendForPrimaryOverMultipleRounds(           
            [Frozen] IHarakaDb harakaDb,
            [Frozen] IMergeProcedure mergeProcedure,
            [Frozen] IHarakaMQMessageBrokerConfiguration harakaMqMessageBrokerConfiguration,
            [Frozen] ISmartQueueFactory smartQueueFactory,
            AntiEntropy sut,
            SmartQueue smartQueue)
        {
            A.CallTo(() => smartQueueFactory.InitializeSmartQueues(A<EventHandler<List<Subscriber>>>.Ignored)).Returns(new List<ISmartQueue>{smartQueue});
            sut.Initialize();
            ConfigureSettings(harakaMqMessageBrokerConfiguration);
            
            foreach (var message in CreateTestMessages(15,5, smartQueue.GetTopicId()))
            {
                sut.PublishMessageReceived(message);
            }
            
            var round1 = sut.GetTentativeMessagesToSendForPrimaryBroker(currentAntiEntropyRound: 1);
            round1.Count.ShouldBe(5);
            round1.TrueForAll(x => x.Packet.AntiEntropyRound == 1);
            
            var round2 = sut.GetTentativeMessagesToSendForPrimaryBroker(currentAntiEntropyRound: 2);
            round2.Count.ShouldBe(5);
            round2.TrueForAll(x => x.Packet.AntiEntropyRound == 2);
            
            var round3 = sut.GetTentativeMessagesToSendForPrimaryBroker(currentAntiEntropyRound: 3);
            round3.Count.ShouldBe(5);
            round3.TrueForAll(x => x.Packet.AntiEntropyRound == 3);

            sut.GetTentativeMessagesToSendForPrimaryBroker(currentAntiEntropyRound: 4).Count.ShouldBe(0);
        }

        private static List<PublishPacketReceivedEventArgs> CreateTestMessages(int numberOfPackets = 1, int totalPacketSizeDevidedBy = 1, string topic = "test")
        {
            var messages = new List<PublishPacketReceivedEventArgs>();

            for (var i = 0; i < numberOfPackets; i++)
            {
                messages.Add(new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet()
                    {
                        GlobalSequenceNumber = i,
                        Size = Setup.TotalPacketSize / totalPacketSizeDevidedBy,
                        Topic = topic
                    }
                });
            }
            return messages;
        }

        private void ConfigureSettings(IHarakaMQMessageBrokerConfiguration harakaMqMessageBrokerConfiguration,
            bool primaryBroker = true)
        {
            A.CallTo(() => harakaMqMessageBrokerConfiguration.PrimaryNumber).Returns(primaryBroker ? 1 : 2);
            A.CallTo(() => harakaMqMessageBrokerConfiguration.BrokerPort).Returns(primaryBroker ? 0 : 123);
            A.CallTo(() => harakaMqMessageBrokerConfiguration.RunInClusterSetup).Returns(true);
            A.CallTo(() => harakaMqMessageBrokerConfiguration.MessageBrokers).Returns(
                new List<global::HarakaMQ.MessageBroker.Models.MessageBroker>
                {
                    new global::HarakaMQ.MessageBroker.Models.MessageBroker
                    {
                        PrimaryNumber = primaryBroker ? 2 : 1,
                        Host = new Host() {IPAddress = "foo", Port =  primaryBroker ? 123 : 0 }
                    }

                });
        }
    }
}