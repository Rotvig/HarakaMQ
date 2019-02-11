using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Models;
using Shouldly;
using Xunit;

namespace HarakaMQ.UnitTests.HarakaMQ.MessageBroker
{
    public class MergeProcedureTests
    {
        private readonly MergeProcedure _mergeProcedure;

        public MergeProcedureTests()
        {
            _mergeProcedure = new MergeProcedure();
        }

        [Fact]
        public void CanCommitStableMessages()
        {
            var foreignTentativeMessages = CreateMixedMessagesToCommit(1); //inside the 3 rounds
            var ownTentativeMessages = CreateMixedMessagesToCommit(4); //outside the 3 rounds

            var committedMessages = new List<PublishPacketReceivedEventArgs>();

            var globalSequenceNumber = 0;
            var lastAntiEntorpyCommit = 0;
            _mergeProcedure.CommitStableMessages(ref committedMessages, ref foreignTentativeMessages, ref ownTentativeMessages, ref lastAntiEntorpyCommit, ref globalSequenceNumber);

            committedMessages.Count.ShouldBe(3);
            committedMessages[0].Packet.GlobalSequenceNumber.ShouldBe(1);
            committedMessages[1].Packet.GlobalSequenceNumber.ShouldBe(2);
            committedMessages[2].Packet.GlobalSequenceNumber.ShouldBe(3);
            foreignTentativeMessages.Count.ShouldBe(3);
            foreignTentativeMessages[0].Packet.ReceivedAtBroker.ShouldBe(new DateTime(4));
            foreignTentativeMessages[1].Packet.ReceivedAtBroker.ShouldBe(new DateTime(5));
            foreignTentativeMessages[2].Packet.ReceivedAtBroker.ShouldBe(new DateTime(6));

            ownTentativeMessages.Count.ShouldBe(6);
            ownTentativeMessages[0].Packet.ReceivedAtBroker.ShouldBe(new DateTime(4));
            ownTentativeMessages[1].Packet.ReceivedAtBroker.ShouldBe(new DateTime(5));
            ownTentativeMessages[2].Packet.ReceivedAtBroker.ShouldBe(new DateTime(6));
            ownTentativeMessages[3].Packet.ReceivedAtBroker.ShouldBe(new DateTime(1));
            ownTentativeMessages[4].Packet.ReceivedAtBroker.ShouldBe(new DateTime(2));
            ownTentativeMessages[5].Packet.ReceivedAtBroker.ShouldBe(new DateTime(3));
        }

        [Fact]
        public void CanMergeMessagesAndOrderByTime()
        {
            var tentativeMessages1 = CreateMessagesWithTime(3);
            var tentativeMessages2 = CreateMessagesWithTime(0);

            var mergeResult = _mergeProcedure.MergeMessages(tentativeMessages1, tentativeMessages2);

            mergeResult.Count.ShouldBe(6);
            mergeResult[0].Packet.ReceivedAtBroker.ShouldBe(new DateTime(1));
            mergeResult[1].Packet.ReceivedAtBroker.ShouldBe(new DateTime(2));
            mergeResult[2].Packet.ReceivedAtBroker.ShouldBe(new DateTime(3));
            mergeResult[3].Packet.ReceivedAtBroker.ShouldBe(new DateTime(4));
            mergeResult[4].Packet.ReceivedAtBroker.ShouldBe(new DateTime(5));
            mergeResult[5].Packet.ReceivedAtBroker.ShouldBe(new DateTime(6));
        }

        private static List<PublishPacketReceivedEventArgs> CreateMixedMessagesToCommit(int antiEntropyRound)
        {
            return new List<PublishPacketReceivedEventArgs>
            {
                new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet
                    {
                        ReceivedAtBroker = new DateTime(4)
                    }
                },
                new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet
                    {
                        ReceivedAtBroker = new DateTime(5)
                    }
                },
                new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet
                    {
                        ReceivedAtBroker = new DateTime(6)
                    }
                },
                new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet
                    {
                        AntiEntropyRound = antiEntropyRound,
                        ReceivedAtBroker = new DateTime(1)
                    }
                },
                new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet
                    {
                        AntiEntropyRound = antiEntropyRound,
                        ReceivedAtBroker = new DateTime(2)
                    }
                },
                new PublishPacketReceivedEventArgs
                {
                    Packet = new Packet
                    {
                        AntiEntropyRound = antiEntropyRound,
                        ReceivedAtBroker = new DateTime(3)
                    }
                },
            };
        }

        private static List<PublishPacketReceivedEventArgs> CreateMessagesWithTime(int startTime)
        {
            return new List<PublishPacketReceivedEventArgs>()
            {
                new PublishPacketReceivedEventArgs()
                {
                    Packet = new Packet
                    {
                        ReceivedAtBroker = new DateTime(startTime + 1)
                    }
                },
                new PublishPacketReceivedEventArgs()
                {
                    Packet = new Packet
                    {
                        ReceivedAtBroker = new DateTime(startTime + 2)
                    }
                },
                new PublishPacketReceivedEventArgs()
                {
                    Packet = new Packet
                    {
                        ReceivedAtBroker = new DateTime(startTime + 3)
                    }
                },
            };
        }
    }
}