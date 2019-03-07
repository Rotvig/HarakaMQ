using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using AutoFixture.Xunit2;
using FakeItEasy;
using HarakaMQ.DB;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UnitTests.Utils;
using Shouldly;
using Xunit;

namespace HarakaMQ.UnitTests.HarakaMQ.UDPCommunication
{
    public class GuranteedDeliveryTests : UnitTestExtension
    {
        [Theory, AutoFakeItEasyData]
        public void CanHandleExtendedMessageInformationAndDeserializeMessageWhenItIsAMessage(
            [Frozen] IReceiver receiver,
            [Frozen] IIdempotentReceiver idempotentReceiver,
            [Frozen] ISender sender,
            [Frozen] IHarakaDb harakaDb,
            GuranteedDelivery sut,
            [Frozen]Packet packet,
            SenderMessage senderMessage,
            ExtendedPacketInformation extendedPacketInformation)
        {
            var ip = "192.1.1.1";
            var port = 1234;
            packet.ReturnPort = port;
            var jsonBytes = MessagePack.MessagePackSerializer.Serialize(new SenderMessage { Type = UdpMessageType.Packet, Body = MessagePack.MessagePackSerializer.Serialize(packet) });
            var udpResult = new UdpReceiveResult(jsonBytes, new IPEndPoint(IPAddress.Parse(ip), port));

            A.CallTo(() => harakaDb.GetObjects<ExtendedPacketInformation>(A<string>.Ignored)).Returns(new List<ExtendedPacketInformation>{extendedPacketInformation});
            A.CallTo(() => idempotentReceiver.VerifyPacket(A<Packet>.Ignored)).Returns(true);

            
            sut.MessageReceived += (object o, ExtendedPacketInformation message) =>
            {
                message.Ip.ShouldBe(ip);
                message.Port.ShouldBe(port);
                message.UdpMessageType.ShouldBe(UdpMessageType.Packet);
                message.Packet.Id.ShouldBe(packet.Id);
            };
            
            sut.HandleExtendedMessageInformation(udpResult);

            A.CallTo(() => harakaDb.StoreObject(A<string>.Ignored, A<List<ExtendedPacketInformation>>.Ignored)).MustHaveHappened();
        }

        [Theory, AutoFakeItEasyData]
        public async void CanRemoveMessagesFromReceiveQueue(
            [Frozen] IReceiver receiver,
            [Frozen] IIdempotentReceiver idempotentReceiver,
            [Frozen] ISender sender,
            [Frozen] IHarakaDb harakaDb,
            GuranteedDelivery sut,
            List<ExtendedPacketInformation> extendedPacketInformations)
        {
            A.CallTo(() => harakaDb.GetObjects<ExtendedPacketInformation>(A<string>.Ignored)).Returns(extendedPacketInformations);

            await sut.RemoveMessageFromReceiveQueueAsync(extendedPacketInformations.First().Id);

            A.CallTo(() => harakaDb.StoreObject(A<string>.Ignored, extendedPacketInformations));
            extendedPacketInformations.Count().ShouldBe(2);
        }

        [Theory, AutoFakeItEasyData]
        public async void CanRemoveMessagesFromSendQueue(
            [Frozen] IReceiver receiver,
            [Frozen] IIdempotentReceiver idempotentReceiver,
            [Frozen] ISender sender,
            [Frozen] IHarakaDb harakaDb,
            GuranteedDelivery sut,
            List<ExtendedPacketInformation> extendedPacketInformations,
            Client client)
        {
            extendedPacketInformations.First().Packet.SeqNo = 1;
            extendedPacketInformations.First().Packet.ReturnPort = client.Port;
            extendedPacketInformations.First().Ip = client.Ip;
            extendedPacketInformations.First().Port = client.Port;

            A.CallTo(() => harakaDb.GetObjects<ExtendedPacketInformation>(A<string>.Ignored)).Returns(extendedPacketInformations);

            await sut.RemoveMessagesFromSendQueueAsync(client.Id, 1);

            A.CallTo(() => harakaDb.StoreObject(A<string>.Ignored, extendedPacketInformations));
            extendedPacketInformations.Count().ShouldBe(2);
        }

        [Theory, AutoFakeItEasyData]
        public void CanResend(
            [Frozen] IReceiver receiver,
            [Frozen] IIdempotentReceiver idempotentReceiver,
            [Frozen] ISender sender,
            [Frozen] IHarakaDb harakaDb,
            GuranteedDelivery sut,
            ExtendedPacketInformation extendedPacketInformation,
            List<ExtendedPacketInformation> extendedPacketInformations,
            Guid id)
        {
            extendedPacketInformation.Packet.Id = id;
            extendedPacketInformations.Add(extendedPacketInformation);
            A.CallTo(() => harakaDb.TryGetObjects<ExtendedPacketInformation>(A<string>.Ignored)).Returns(extendedPacketInformations);

            sut.ReSend(id);

            A.CallTo(() => sender.SendMsg(A<SenderMessage>.Ignored, extendedPacketInformation.Ip, extendedPacketInformation.Port)).MustHaveHappened();
        }

        [Theory, AutoFakeItEasyData]
        public async void CanSendMessage(
            [Frozen] IReceiver receiver,
            [Frozen] IIdempotentReceiver idempotentReceiver,
            [Frozen] ISender sender,
            [Frozen] IHarakaDb harakaDb,
            GuranteedDelivery sut,
            ExtendedPacketInformation extendedPacketInformation,
            List<ExtendedPacketInformation> extendedPacketInformations,
            Guid id)
        {
            extendedPacketInformation.Packet.Id = id;
            A.CallTo(() => harakaDb.TryGetObjects<ExtendedPacketInformation>(A<string>.Ignored)).Returns(extendedPacketInformations);

            await sut.Send(extendedPacketInformation);

            A.CallTo(() => sender.SendMsg(A<SenderMessage>.Ignored, extendedPacketInformation.Ip, extendedPacketInformation.Port)).MustHaveHappened();
        }
    }
}