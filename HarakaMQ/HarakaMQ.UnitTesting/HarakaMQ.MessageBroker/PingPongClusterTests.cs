using System.Collections.Generic;
using FakeItEasy;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
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
            _settings = A.Fake<IJsonConfigurator>();
            _timeSyncProtocol = A.Fake<ITimeSyncProtocol>();

            ConfigureSettings();
            _pingPong = new PingPong(_udpCommunication, _schedular, _antiEntropy, _settings, _timeSyncProtocol);
        }

        private readonly PingPong _pingPong;
        private readonly IUdpCommunication _udpCommunication;
        private readonly ISchedular _schedular;
        private readonly IAntiEntropy _antiEntropy;
        private readonly IJsonConfigurator _settings;
        private readonly ITimeSyncProtocol _timeSyncProtocol;

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
                        Ipaddress = "foo",
                    }
                }
            });
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsNonPrimaryBroker()
        {
            ConfigureSettings(false);
            var pingPongNonPrimaryBroker = new PingPong(_udpCommunication, _schedular, _antiEntropy, _settings, _timeSyncProtocol);
            var messageReceived = new MessageReceivedEventArgs
            {
                Port = 0,
                IpAddress = "foo",
                AdministrationMessage = new AdministrationMessage
                {
                    Data = MessagePackSerializer.Serialize(new AntiEntropyMessage
                    {
                        PrimaryNumber = 1,
                        Committed = new List<PublishPacketReceivedEventArgs>(),
                        Tentative = new List<PublishPacketReceivedEventArgs>()
                    })
                }
            };

            pingPongNonPrimaryBroker.AntiEntropyMessageReceived(messageReceived);

            A.CallTo(() => _antiEntropy.AntiEntropyNonPrimaryMessageReceived(A<AntiEntropyMessage>._)).MustHaveHappened();
            A.CallTo(() => _udpCommunication.SendAdministrationMessage(A<AdministrationMessage>._, "foo", 0)).MustHaveHappened();
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsrimaryBroker()
        {
            var messageReceived = new MessageReceivedEventArgs
            {
                Port = 123,
                IpAddress = "foo",
                AdministrationMessage = new AdministrationMessage
                {
                    Data = MessagePackSerializer.Serialize(new AntiEntropyMessage
                    {
                        PrimaryNumber = 2,
                        AntiEntropyRound = 0,
                        Committed = new List<PublishPacketReceivedEventArgs>(),
                        Tentative = new List<PublishPacketReceivedEventArgs>()
                    })
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
            var messageReceived = new MessageReceivedEventArgs
            {
                Port = 123,
                IpAddress = "foo",
                AdministrationMessage = new AdministrationMessage
                {
                    Data = MessagePackSerializer.Serialize(new AntiEntropyMessage
                    {
                        PrimaryNumber = 2,
                        AntiEntropyRound = 3,
                        Tentative = new List<PublishPacketReceivedEventArgs>()
                    })
                }
            };
            _pingPong.StartGossip();
            _pingPong.AntiEntropyMessageReceived(messageReceived);
            var i = 0;
            A.CallTo(() => _antiEntropy.AntiEntropyCommitStableMessages(ref i, ref i)).MustHaveHappened();
        }
    }
}