using System;
using System.Collections.Generic;
using FakeItEasy;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UnitTests.Utils;
using Shouldly;
using Xunit;
using Setup = HarakaMQ.UDPCommunication.Utils.Setup;

namespace HarakaMQ.UnitTests.HarakaMQ.UDPCommunication
{
    public class AutomaticRepeatReQuestTests : UnitTestExtension
    {
        public AutomaticRepeatReQuestTests()
        {
            _guranteedDelivery = A.Fake<IGuranteedDelivery>();
            _schedularFake = _schedularFake = A.Fake<ISchedular>();
            HarakaDb.CreateFiles(_clients);
            _automaticRepeatReQuest = new AutomaticRepeatReQuest(_guranteedDelivery, _schedularFake, HarakaDb);
            Setup.ClientsCS = _clients;
        }

        private readonly AutomaticRepeatReQuest _automaticRepeatReQuest;
        private readonly IGuranteedDelivery _guranteedDelivery;
        private readonly ISchedular _schedularFake;
        private readonly string _clients = "Clients";

        private static ExtendedPacketInformation CreateExtendedMessageInformation(Client client, uint messageSeqNo)
        {
            throw new NotImplementedException();

            //var extendedMessageInformation = new ExtendedPacketInformation(Guid.NewGuid());
            //extendedMessageInformation.SetUdpMessageType(UdpMessageType.Packet);
            //var message = new AdministrationMessage(Guid.NewGuid());
            //message.SetReturnPort(client.Port);
            //extendedMessageInformation.Ip = client.Ip;
            //message.SetSeqNo(messageSeqNo);
            //extendedMessageInformation.SetMessage(message);
            //extendedMessageInformation.SetIpAndPort(client.Ip, client.Port);
            //return extendedMessageInformation;
        }

        [Fact]
        public void CanHandleRecievedMessageOfTypeMessage()
        {
            throw new NotImplementedException();

            //Setup.AckAfterNumber = 50;
            //var client = new Client { Ip = "foo", Port = 1 };
            //client.SetIngoingSeqNo(0);
            //HarakaDb.StoreObject(_clients, new List<Client> { client });

            //var extendedMessageInformation = new ExtendedPacketInformation();
            //extendedMessageInformation.SetUdpMessageType(UdpMessageType.Packet);
            //var message = new AdministrationMessage();
            //message.SetSeqNo(1);
            //message.SetReturnPort(client.Port);
            //extendedMessageInformation.Ip = client.Ip;
            //extendedMessageInformation.Port = client.Port;
            //extendedMessageInformation.SetMessage(message);

            //_automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation);

            //A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation.Id)).MustHaveHappened();
            //var clientResult = HarakaDb.TryGetObjects<Client>(_clients).Find(x => x.Id == client.Id);
            //clientResult.IngoingSeqNo.ShouldBe(1u);
        }

        [Fact]
        public void CanHandleRecievedMessageOfTypeMessageMixedInAndOutOfOrder()
        {
            Setup.AckAfterNumber = 50;
            var client = new Client { Ip = "foo", Port = 1 };
            client.SetIngoingSeqNo(0);
            HarakaDb.StoreObject(_clients, new List<Client> { client });

            //Should pass through first time
            var extendedMessageInformation1 = CreateExtendedMessageInformation(client, 1);
            var extendedMessageInformation2 = CreateExtendedMessageInformation(client, 2);
            var extendedMessageInformation3 = CreateExtendedMessageInformation(client, 3);

            //Should be added to out of order dictionary
            var extendedMessageInformation5 = CreateExtendedMessageInformation(client, 5);
            var extendedMessageInformation6 = CreateExtendedMessageInformation(client, 6);
            var extendedMessageInformation7 = CreateExtendedMessageInformation(client, 7);
            var extendedMessageInformation9 = CreateExtendedMessageInformation(client, 9);

            //Should make 5,6,7 pass and not 9
            var extendedMessageInformation4 = CreateExtendedMessageInformation(client, 4);

            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation1);
            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation2);
            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation3);

            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation1.Id)).MustHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation2.Id)).MustHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation3.Id)).MustHaveHappened();

            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation5);
            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation6);
            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation7);
            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation9);

            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation5.Id)).MustNotHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation6.Id)).MustNotHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation7.Id)).MustNotHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation9.Id)).MustNotHaveHappened();

            _automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation4);
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation5.Id)).MustHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation6.Id)).MustHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation7.Id)).MustHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation4.Id)).MustHaveHappened();
            A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedMessageInformation9.Id)).MustNotHaveHappened();

            var clientResult = HarakaDb.TryGetObjects<Client>(_clients).Find(x => x.Id == client.Id);
            clientResult.IngoingSeqNo.ShouldBe(7);
            _automaticRepeatReQuest.DictionaryWithSortedMessages.Count.ShouldBe(1);
            var DictionaryWithSortedMessage = _automaticRepeatReQuest.DictionaryWithSortedMessages[client.Id];
            DictionaryWithSortedMessage.Keys.ShouldContain(9);
            DictionaryWithSortedMessage.Values[0].Id.ShouldBe(extendedMessageInformation9.Id);
        }


        [Fact]
        public void CanHandleRecievedMessageOfTypeMessageOutOfOrder()
        {
            throw new NotImplementedException();

            //var client = new Client { Ip = "foo", Port = 1 };
            //client.SetIngoingSeqNo(0);
            //HarakaDb.StoreObject(_clients, new List<Client> { client });

            //var extendedMessageInformation = CreateExtendedMessageInformation(client, 2);
            //_automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation);

            //A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(A<Guid>._)).MustNotHaveHappened();
            //var clientResult = HarakaDb.TryGetObjects<Client>(_clients).Find(x => x.Id == client.Id);
            //clientResult.IngoingSeqNo.ShouldBe(0u);
            //_automaticRepeatReQuest.DictionaryWithSortedMessages.Count.ShouldBe(1);
            //var DictionaryWithSortedMessage = _automaticRepeatReQuest.DictionaryWithSortedMessages[client.Id];
            //DictionaryWithSortedMessage.Keys.ShouldContain(2u);
            //DictionaryWithSortedMessage.Values[0].AdministrationMessage.Id.ShouldBe(extendedMessageInformation.AdministrationMessage.Id);
        }

        [Fact]
        public void CanHandleRecievedMessageSyncMessage()
        {
            //Setup.AckAfterNumber = 50;
            //var client = new Client { Ip = "foo", Port = 1 };
            //client.SetIngoingSeqNo(0);
            //HarakaDb.StoreObject(_clients, new List<Client> { client });

            //var extendedMessageInformation = new ExtendedPacketInformation();
            //extendedMessageInformation.SetUdpMessageType(UdpMessageType.Packet);
            //var message = new AdministrationMessage();
            //message.SetAsSynMessage();
            //message.SetSeqNo(42);
            //message.SetReturnPort(client.Port);
            //extendedMessageInformation.Ip = client.Ip;
            //extendedMessageInformation.Port = client.Port;
            //extendedMessageInformation.SetMessage(message);
            //_automaticRepeatReQuest.HandleRecievedMessage(extendedMessageInformation);

            //A.CallTo(() => _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(A<Guid>._)).MustHaveHappened();
            //var clientResult = HarakaDb.TryGetObjects<Client>(_clients).Find(x => x.Id == client.Id);
            //clientResult.IngoingSeqNo.ShouldBe(42u);
            //_automaticRepeatReQuest.DictionaryWithSortedMessages.Count.ShouldBe(1);
            //var DictionaryWithSortedMessage = _automaticRepeatReQuest.DictionaryWithSortedMessages[client.Id];
            //DictionaryWithSortedMessage.Count.ShouldBe(0);
        }

        [Fact]
        public void CanSendMessage()
        {
            throw new NotImplementedException();

            //var client = new Client("foo", 123);
            //client.SetOutgoingSeqNo(4);
            //client.SetInSync(true);
            //HarakaDb.StoreObject(_clients, new List<Client> { client });

            //var message = new AdministrationMessage();
            //_automaticRepeatReQuest.Send(message, "foo", 123);

            //A.CallTo(() => _guranteedDelivery.Send(A<ExtendedPacketInformation>.That.Matches(
            //    x => x.AdministrationMessage == message &&
            //    x.AdministrationMessage.SeqNo == 5 &&
            //    x.Ip == "foo" &&
            //    x.Port == 123))).MustHaveHappened();

            //var clientResult = HarakaDb.TryGetObjects<Client>(_clients).Find(x => x.Id == client.Id);
            //clientResult.OutgoingSeqNo.ShouldBe(5u);
        }

        [Fact]
        public void CanSendMessageInSync()
        {
            throw new NotImplementedException();
            //var client = new Client("foo", 123);
            //client.SetOutgoingSeqNo(6);
            //client.SetInSync(false);
            //HarakaDb.StoreObject(_clients, new List<Client> { client });


            //var message = new AdministrationMessage();
            //_automaticRepeatReQuest.Send(message, "foo", 123);

            //A.CallTo(() => _guranteedDelivery.Send(A<ExtendedPacketInformation>.That.Matches(
            //    x => x.AdministrationMessage == message &&
            //         x.AdministrationMessage.SeqNo == 7 &&
            //         x.AdministrationMessage.Sync &&
            //         x.Ip == "foo" &&
            //         x.Port == 123))).MustHaveHappened();

            //var clientResult = HarakaDb.TryGetObjects<Client>(_clients).Find(x => x.Id == client.Id);
            //clientResult.InSync.ShouldBe(true);
            //clientResult.OutgoingSeqNo.ShouldBe(7u);
        }
    }
}