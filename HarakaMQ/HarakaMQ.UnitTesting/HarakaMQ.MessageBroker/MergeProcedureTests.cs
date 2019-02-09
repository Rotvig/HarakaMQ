using System;
using System.Collections.Generic;
using HarakaMQ.MessageBroker;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Models;
using Shouldly;
using Xunit;

namespace HarakaMQ.UnitTesting.HarakaMQ.MessageBroker
{
    public class MergeProcedureTests
    {
        public MergeProcedureTests()
        {
            _mergeProcedure = new MergeProcedure();
        }

        private readonly MergeProcedure _mergeProcedure;

        private static List<MessageReceivedEventArgs> CreateMixedMessagesToCommit(int antiEntropyRound)
        {
            throw new NotImplementedException();

            //return new List<MessageReceivedEventArgs>
            //{
            //    new MessageReceivedEventArgs
            //    {
            //        AdministrationMessage = new Message()
            //        {
            //            ReceivedAtBroker = TimeSpan.FromMilliseconds(4)
            //        }
            //    },
            //    new MessageReceivedEventArgs
            //    {
            //        AdministrationMessage = new Message()
            //        {
            //            ReceivedAtBroker = TimeSpan.FromMilliseconds(5)
            //        }
            //    },
            //    new MessageReceivedEventArgs
            //    {
            //        AdministrationMessage = new Message()
            //        {
            //            ReceivedAtBroker = TimeSpan.FromMilliseconds(6)
            //        }
            //    },
            //    new MessageReceivedEventArgs
            //    {
            //        AdministrationMessage = new Message
            //        {
            //            AntiEntropyRound = antiEntropyRound,
            //            ReceivedAtBroker = TimeSpan.FromMilliseconds(1)
            //        }
            //    },
            //    new MessageReceivedEventArgs
            //    {
            //        AdministrationMessage = new Message
            //        {
            //            AntiEntropyRound = antiEntropyRound,
            //            ReceivedAtBroker = TimeSpan.FromMilliseconds(2)
            //        }
            //    },
            //    new MessageReceivedEventArgs
            //    {
            //        AdministrationMessage = new Message
            //        {
            //            AntiEntropyRound = antiEntropyRound,
            //            ReceivedAtBroker = TimeSpan.FromMilliseconds(3)
            //        }
            //    }
            //};
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

        [Fact]
        public void CanCommitStableMessages()
        {
            throw new NotImplementedException();
            //var foreignTentativeMessages = CreateMixedMessagesToCommit(1); //inside the 3 rounds
            //var ownTentativeMessages = CreateMixedMessagesToCommit(4); //outside the 3 rounds

            //var committedMessages = new List<MessageReceivedEventArgs>();

            //var globalSequenceNumber = 0;
            //var lastAntiEntorpyCommit = 0;
            //_mergeProcedure.CommitStableMessages(ref committedMessages, ref foreignTentativeMessages, ref ownTentativeMessages, ref lastAntiEntorpyCommit, ref globalSequenceNumber);

            //committedMessages.Count.ShouldBe(3);
            //committedMessages[0].AdministrationMessage.GlobalSequenceNumber.ShouldBe(1);
            //committedMessages[1].AdministrationMessage.GlobalSequenceNumber.ShouldBe(2);
            //committedMessages[2].AdministrationMessage.GlobalSequenceNumber.ShouldBe(3);
            //foreignTentativeMessages.Count.ShouldBe(3);
            //foreignTentativeMessages[0].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(4));
            //foreignTentativeMessages[1].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(5));
            //foreignTentativeMessages[2].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(6));

            //ownTentativeMessages.Count.ShouldBe(6);
            //ownTentativeMessages[0].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(4));
            //ownTentativeMessages[1].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(5));
            //ownTentativeMessages[2].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(6));
            //ownTentativeMessages[3].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(1));
            //ownTentativeMessages[4].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(2));
            //ownTentativeMessages[5].AdministrationMessage.ReceivedAtBroker.ShouldBe(TimeSpan.FromMilliseconds(3));
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
    }
}