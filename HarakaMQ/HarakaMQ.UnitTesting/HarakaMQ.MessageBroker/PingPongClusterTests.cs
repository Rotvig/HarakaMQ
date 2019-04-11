using System;
using System.Collections.Generic;
using FakeItEasy;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using MessagePack;
using Xunit;

namespace HarakaMQ.UnitTests.HarakaMQ.MessageBroker
{
    public class PingPongClusterTests
    {
        public PingPongClusterTests()
        {
            _udpCommunication = A.Fake<IUdpCommunication>();
            _schedular = A.Fake<ISchedular>();
            _antiEntropy = A.Fake<IAntiEntropy>();
            _timeSyncProtocol = A.Fake<ITimeSyncProtocol>();
            _harakaMqMessageBrokerConfiguration = A.Fake<IHarakaMQMessageBrokerConfiguration>();
            _serializer= A.Fake<ISerializer>();

            ConfigureSettings();
            _pingPong = new PingPong(_udpCommunication, _schedular, _antiEntropy, _harakaMqMessageBrokerConfiguration, _timeSyncProtocol, _serializer);
        }

        private readonly PingPong _pingPong;
        private readonly IUdpCommunication _udpCommunication;
        private readonly ISchedular _schedular;
        private readonly IAntiEntropy _antiEntropy;
        private readonly ITimeSyncProtocol _timeSyncProtocol;
        private IHarakaMQMessageBrokerConfiguration _harakaMqMessageBrokerConfiguration;
        private ISerializer _serializer;

        private void ConfigureSettings(bool primaryBroker = true)
        {
            A.CallTo(() => _harakaMqMessageBrokerConfiguration.PrimaryNumber).Returns(primaryBroker ? 1 : 2);
            A.CallTo(() => _harakaMqMessageBrokerConfiguration.BrokerPort).Returns(primaryBroker ? 0 : 123);
            A.CallTo(() => _harakaMqMessageBrokerConfiguration.RunInClusterSetup).Returns(true);
            A.CallTo(() => _harakaMqMessageBrokerConfiguration.MessageBrokers).Returns(new List<global::HarakaMQ.MessageBroker.Models.MessageBroker>
            {
                new global::HarakaMQ.MessageBroker.Models.MessageBroker
                {
                    PrimaryNumber = primaryBroker ? 2 : 1,
                    Host = new Host()
                    {
                        Port = primaryBroker ? 123 : 0,
                        IPAddress = "foo" 
                    },
                }});
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsNonPrimaryBroker()
        {
            ConfigureSettings(false);
            var pingPongNonPrimaryBroker = new PingPong(_udpCommunication, _schedular, _antiEntropy, _harakaMqMessageBrokerConfiguration, _timeSyncProtocol, _serializer);
            A.CallTo(() => _serializer.Deserialize<AntiEntropyMessage>(A<Byte[]>._)).Returns(
                new AntiEntropyMessage
                {
                    PrimaryNumber = 1,
                    Committed = new List<PublishPacketReceivedEventArgs>(),
                    Tentative = new List<PublishPacketReceivedEventArgs>()
                });
            var messageReceived = new MessageReceivedEventArgs
            {
                Port = 0,
                IpAddress = "foo",
                AdministrationMessage = new AdministrationMessage
                {
                    Data = new byte[0]
                }
            };

            pingPongNonPrimaryBroker.AntiEntropyMessageReceived(messageReceived);

            A.CallTo(() => _antiEntropy.AntiEntropyNonPrimaryMessageReceived(A<AntiEntropyMessage>._)).MustHaveHappened();
            A.CallTo(() => _udpCommunication.SendAdministrationMessage(A<AdministrationMessage>._, A<Host>._)).MustHaveHappened();
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsrimaryBroker()
        {
            A.CallTo(() => _serializer.Deserialize<AntiEntropyMessage>(A<Byte[]>._)).Returns(
                new AntiEntropyMessage
                {
                    PrimaryNumber = 2,
                    AntiEntropyRound = 0,
                    Committed = new List<PublishPacketReceivedEventArgs>(),
                    Tentative = new List<PublishPacketReceivedEventArgs>()
                });
            
            var messageReceived = new MessageReceivedEventArgs
            {
                Port = 123,
                IpAddress = "foo",
                AdministrationMessage = new AdministrationMessage
                {
                    Data = new byte[0]
                }
            };
            
            _pingPong.StartGossip();
            _pingPong.AntiEntropyMessageReceived(messageReceived);
            
            var i = 0;
            A.CallTo(() => _antiEntropy.AntiEntropyAddTentativeMessages(A<AntiEntropyMessage>._, ref i, ref i)).MustHaveHappened();
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsrimaryBrokerAfterThreeRoundsOfAntiEntropy()
        {
            A.CallTo(() => _serializer.Deserialize<AntiEntropyMessage>(A<Byte[]>._)).Returns(
                new AntiEntropyMessage
                {
                    PrimaryNumber = 2,
                    AntiEntropyRound = 3,
                    Tentative = new List<PublishPacketReceivedEventArgs>()
                });
            
            var messageReceived = new MessageReceivedEventArgs
            {
                Port = 123,
                IpAddress = "foo",
                AdministrationMessage = new AdministrationMessage
                {
                    Data = new byte[0]
                }
            };
            
            _pingPong.StartGossip();
            _pingPong.AntiEntropyMessageReceived(messageReceived);
            
            var i = 0;
            A.CallTo(() => _antiEntropy.AntiEntropyCommitStableMessages(ref i, ref i)).MustHaveHappened();
        }
    }
}