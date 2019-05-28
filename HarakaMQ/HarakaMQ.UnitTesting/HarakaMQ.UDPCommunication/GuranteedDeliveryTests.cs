using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using AutoFixture.Xunit2;
using FakeItEasy;
using HarakaMQ.DB;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UnitTests.Utils;
using Shouldly;
using Xunit;

namespace HarakaMQ.UnitTests.HarakaMQ.UDPCommunication
{
    public class GuranteedDeliveryTests
    {
        [Theory, AutoFakeItEasyData]
        public void CanHandleExtendedMessageInformationAndDeserializeMessageWhenItIsAMessage(
            [Frozen] IReceiver receiver,
            [Frozen] IIdempotentReceiver idempotentReceiver,
            [Frozen] ISender sender,
            [Frozen] ISerializer serializer,
            [Frozen] IHarakaDb harakaDb,
            GuranteedDelivery sut,
            [Frozen]Packet packet,
            SenderMessage senderMessage,
            ExtendedPacketInformation extendedPacketInformation,
            UdpReceiveResult udpReceiveResult)
        {
            packet.ReturnPort = udpReceiveResult.RemoteEndPoint.Port;
            senderMessage.Type = UdpMessageType.Packet;
            extendedPacketInformation.Host.IPAddress = udpReceiveResult.RemoteEndPoint.Address.ToString();
            extendedPacketInformation.Host.Port = udpReceiveResult.RemoteEndPoint.Port;

            A.CallTo(() => serializer.Deserialize<SenderMessage>(udpReceiveResult.Buffer)).Returns(senderMessage);
            A.CallTo(() => serializer.Deserialize<Packet>(senderMessage.Body)).Returns(packet);
            A.CallTo(() => harakaDb.GetObjects<ExtendedPacketInformation>(A<string>.Ignored)).Returns(new List<ExtendedPacketInformation>{extendedPacketInformation});
            A.CallTo(() => idempotentReceiver.VerifyPacket(A<Packet>.Ignored)).Returns(true);

            sut.MessageReceived += (object o, ExtendedPacketInformation message) =>
            {
                message.Host.IPAddress.ShouldBe(extendedPacketInformation.Host.IPAddress);
                message.Host.Port.ShouldBe(extendedPacketInformation.Host.Port);
                message.UdpMessageType.ShouldBe(UdpMessageType.Packet);
                message.Packet.Id.ShouldBe(packet.Id);
            };
            
            sut.HandleExtendedMessageInformation(udpReceiveResult);

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
            extendedPacketInformations.First().Packet.ReturnPort = client.Host.Port;
            extendedPacketInformations.First().Host.IPAddress = client.Host.IPAddress;
            extendedPacketInformations.First().Host.Port = client.Host.Port;

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

            A.CallTo(() => sender.SendMsg(A<SenderMessage>.Ignored, extendedPacketInformation.Host.IPAddress, extendedPacketInformation.Host.Port)).MustHaveHappened();
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

            A.CallTo(() => sender.SendMsg(A<SenderMessage>.Ignored, extendedPacketInformation.Host.IPAddress, extendedPacketInformation.Host.Port)).MustHaveHappened();
        }
    }
}