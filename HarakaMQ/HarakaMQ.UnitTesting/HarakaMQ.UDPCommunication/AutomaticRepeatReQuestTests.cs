using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FakeItEasy;
using HarakaMQ.DB;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using HarakaMQ.UnitTests.Utils;
using Shouldly;
using Xunit;
using Setup = HarakaMQ.UDPCommunication.Utils.Setup;

namespace HarakaMQ.UnitTests.HarakaMQ.UDPCommunication
{
    public class AutomaticRepeatReQuestTests
    {
        [Theory, AutoFakeItEasyDataAttributeWithExtendedPacketInformationSpecimenBuilder]
        public async void CanHandleRecievedMessageOfTypeMessage(
            [Frozen] IGuranteedDelivery guranteedDelivery,
            [Frozen] ISchedular schedular,
            [Frozen] IHarakaDb harakaDb,
            [Frozen] HarakaMQUDPConfiguration harakaMqudpConfiguration,
            AutomaticRepeatReQuest sut,
            ExtendedPacketInformation extendedPacketInformation,
            Client client)
        {
            harakaMqudpConfiguration.AcknowledgeMessageAfterNumberOfMessages = 1;
            client.IngoingSeqNo = 0;
            client.Host.IPAddress = extendedPacketInformation.Host.IPAddress;
            client.Host.Port = extendedPacketInformation.Host.Port;

            A.CallTo(() => harakaDb.GetObjects<Client>(A<string>.Ignored)).Returns(new List<Client>() {client});

            await sut.HandleRecievedMessage(extendedPacketInformation);
            
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation.Id)).MustHaveHappened();
            A.CallTo(() => harakaDb.GetObjects<Client>(A<string>.Ignored)).MustHaveHappened();
            A.CallTo(() => harakaDb.StoreObject(A<string>.Ignored, A<List<Client>>.Ignored)).MustHaveHappened();
        }

        [Theory, AutoFakeItEasyDataAttributeWithExtendedPacketInformationSpecimenBuilder]
        public async void CanHandleRecievedMessageOfTypeMessageMixedInAndOutOfOrder(
            [Frozen] IGuranteedDelivery guranteedDelivery,
            [Frozen] ISchedular schedular,
            [Frozen] IHarakaDb harakaDb,
            [Frozen] IHarakaMQUDPConfiguration harakaMqudpConfiguration,
            AutomaticRepeatReQuest sut,
            ExtendedPacketInformation extendedPacketInformation1,
            ExtendedPacketInformation extendedPacketInformation2,
            ExtendedPacketInformation extendedPacketInformation3,
            ExtendedPacketInformation extendedPacketInformation4,
            ExtendedPacketInformation extendedPacketInformation5,
            ExtendedPacketInformation extendedPacketInformation6,
            ExtendedPacketInformation extendedPacketInformation7,
            ExtendedPacketInformation extendedPacketInformation8,
            ExtendedPacketInformation extendedPacketInformation9,
            Client client)
        {
            harakaMqudpConfiguration.AcknowledgeMessageAfterNumberOfMessages = 1;
            client.IngoingSeqNo = 0;
            client.Host.IPAddress = extendedPacketInformation1.Host.IPAddress;
            client.Host.Port = extendedPacketInformation1.Host.Port;
            
            A.CallTo(() => harakaDb.GetObjects<Client>(A<string>.Ignored)).Returns(new List<Client>() {client});

            await sut.HandleRecievedMessage(extendedPacketInformation1);
            await sut.HandleRecievedMessage(extendedPacketInformation2);
            await sut.HandleRecievedMessage(extendedPacketInformation3);

            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation1.Id)).MustHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation2.Id)).MustHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation3.Id)).MustHaveHappened();

            await sut.HandleRecievedMessage(extendedPacketInformation5);
            await sut.HandleRecievedMessage(extendedPacketInformation6);
            await sut.HandleRecievedMessage(extendedPacketInformation7);
            await sut.HandleRecievedMessage(extendedPacketInformation9);

            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation5.Id)).MustNotHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation6.Id)).MustNotHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation7.Id)).MustNotHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation9.Id)).MustNotHaveHappened();

            await sut.HandleRecievedMessage(extendedPacketInformation4);
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation5.Id)).MustHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation6.Id)).MustHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation7.Id)).MustHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation4.Id)).MustHaveHappened();
            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation9.Id)).MustNotHaveHappened();
            
            client.IngoingSeqNo.ShouldBe(7);
        }

        [Theory, AutoFakeItEasyDataAttributeWithExtendedPacketInformationSpecimenBuilder]
        public async void CanHandleRecievedMessageOfTypeMessageOutOfOrder(
            [Frozen] IGuranteedDelivery guranteedDelivery,
            [Frozen] ISchedular schedular,
            [Frozen] IHarakaDb harakaDb,
            AutomaticRepeatReQuest sut,
            ExtendedPacketInformation extendedPacketInformation)
        {
            extendedPacketInformation.Packet.SeqNo = 2;
            
            await sut.HandleRecievedMessage(extendedPacketInformation);

            A.CallTo(() => guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation.Id)).MustNotHaveHappened();
        }

        [Theory, AutoFakeItEasyData]
        public async void CanSendMessage(
            [Frozen] IGuranteedDelivery guranteedDelivery,
            [Frozen] ISchedular schedular,
            [Frozen] IHarakaDb harakaDb,
            AutomaticRepeatReQuest sut,
            Client client,
            Message message,
            Host host)
        {
            A.CallTo(() => harakaDb.GetObjects<Client>(A<string>.Ignored)).Returns(new List<Client> {client});

           await sut.Send(message, "topic", host);

            A.CallTo(() => guranteedDelivery.Send(A<ExtendedPacketInformation>.That.Matches(x => x.Host.IPAddress == host.IPAddress && x.Host.Port == host.Port))).MustHaveHappened();
        }
    }
}