using System;
using System.Collections.Generic;
using FakeItEasy;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UnitTesting.Utils;
using Shouldly;
using Xunit;
using Setup = HarakaMQ.UDPCommunication.Utils.Setup;

namespace HarakaMQ.UnitTesting.HarakaMQ.UDPCommunication
{
    public class GuranteedDeliveryTests : UnitTestExtension
    {
        public GuranteedDeliveryTests()
        {
            _recieverFake = A.Fake<IReceiver>();
            _idempotentReceiver = A.Fake<IIdempotentReceiver>();
            _senderFake = A.Fake<ISender>();

            HarakaDb.CreateFiles(outgoingmessages, ingoingmessages);
            _guranteedDelivery = new GuranteedDelivery(_senderFake, _recieverFake, _idempotentReceiver, HarakaDb);
            Setup.IngoingMessagesCS = ingoingmessages;
            Setup.OutgoingMessagesCS = outgoingmessages;
            throw new NotImplementedException();

            //A.CallTo(() => _idempotentReceiver.VerifyPacket(A<AdministrationMessage>._)).Returns(true);
        }

        private readonly GuranteedDelivery _guranteedDelivery;
        private readonly IReceiver _recieverFake;
        private readonly ISender _senderFake;
        private readonly IIdempotentReceiver _idempotentReceiver;

        private readonly string ingoingmessages = "InGoingMessages";
        private readonly string outgoingmessages = "OutgoingMessages";

        [Fact]
        public void CanHandleExtendedMessageInformationAndDeserializeMessageWhenItIsAMessage()
        {
            throw new NotImplementedException();

            //var msg = new AdministrationMessage();
            //msg.SetReturnPort(321);
            //var message = new ExtendedPacketInformation(msg, UdpMessageType.Packet, "192.111.1.1", 123);
            //var jsonBytes = JsonSerializer.Serialize(new SenderMessage { Type = message.UdpMessageType, Body = JsonSerializer.Serialize(message.AdministrationMessage) });
            //var udpResult = new UdpReceiveResult(jsonBytes, new IPEndPoint(IPAddress.Parse(message.Ip), message.Port));

            //_guranteedDelivery.MessageReceived += (sender, information) =>
            //{
            //    information.Ip.ShouldBe(message.Ip);
            //    information.Port.ShouldBe(msg.ReturnPort);
            //    information.UdpMessageType.ShouldBe(message.UdpMessageType);
            //    information.AdministrationMessage.Id.ShouldBe(message.AdministrationMessage.Id);
            //};

            //_guranteedDelivery.HandleExtendedMessageInformation(udpResult);

            //var messages = HarakaDb.GetObjects<ExtendedPacketInformation>(ingoingmessages);
            //messages.Count.ShouldBe(1);
            //var resultMessage = messages.First();
            //resultMessage.UdpMessageType.ShouldBe(UdpMessageType.Packet);
            //resultMessage.Ip.ShouldBe("192.111.1.1");
            //resultMessage.Port.ShouldBe(msg.ReturnPort);
        }

        [Fact]
        public void CanRemoveMessagesFromReceiveQueue()
        {
            var message = new ExtendedPacketInformation();
            HarakaDb.StoreObject(ingoingmessages, new List<ExtendedPacketInformation> {message});
            _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(message.Id).RunSynchronously();

            var messages = HarakaDb.TryGetObjects<ExtendedPacketInformation>(ingoingmessages);
            messages.Count.ShouldBe(0);
            messages.ShouldNotContain(x => x.Id == message.Id);
        }

        [Fact]
        public void CanRemoveMessagesFromSendQueue()
        {
            throw new NotImplementedException();

            //var message = new ExtendedPacketInformation
            //{
            //    AdministrationMessage = new AdministrationMessage() { SeqNo = 1 },
            //    Ip = "foo",
            //    Port = 1,
            //};
            //HarakaDb.StoreObject(outgoingmessages, new List<ExtendedPacketInformation> { message });

            //_guranteedDelivery.RemoveMessagesFromSendQueueAsync(message.Ip + message.Port, 1).RunSynchronously();

            //var messages = HarakaDb.TryGetObjects<ExtendedPacketInformation>(outgoingmessages);
            //messages.Count.ShouldBe(0);
            //messages.ShouldNotContain(x => x.Id == message.Id);
        }

        [Fact]
        public void CanResend()
        {
            throw new NotImplementedException();

            //var extendedMessage = new ExtendedPacketInformation { AdministrationMessage = new AdministrationMessage(), Ip = "foo", Port = 1};
            //HarakaDb.StoreObject(Setup.OutgoingMessagesCS, new List<ExtendedPacketInformation>() { extendedMessage });

            //_guranteedDelivery.ReSend(extendedMessage.AdministrationMessage.Id);

            //A.CallTo(() => _senderFake.SendMsg(A<SenderMessage>._, "foo", 1)).MustHaveHappened();
        }

        [Fact]
        public void CanSendMessage()
        {
            var extendedMessage = new ExtendedPacketInformation();
            extendedMessage.SetIpAndPort("foo", 123);
            extendedMessage.SetId(Guid.NewGuid());

            _guranteedDelivery.Send(extendedMessage);

            A.CallTo(() => _senderFake.SendMsg(A<SenderMessage>._, "foo", 123)).MustHaveHappened();
            var messages = HarakaDb.TryGetObjects<ExtendedPacketInformation>(outgoingmessages);
            messages.Count.ShouldBe(1);
            messages.ShouldContain(x => x.Id == extendedMessage.Id);
            var message = messages.Find(x => x.Id == extendedMessage.Id);
            message.Ip.ShouldBe("foo");
            message.Port.ShouldBe(123);
        }
    }
}