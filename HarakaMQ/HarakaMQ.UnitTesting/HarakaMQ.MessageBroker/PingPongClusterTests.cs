using System;
using System.Collections.Generic;
using FakeItEasy;
using HarakaMQ.MessageBroker;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.MessageBroker.Utils;
using HarakaMQ.UDPCommunication.Interfaces;
using Xunit;

namespace HarakaMQ.UnitTesting.HarakaMQ.MessageBroker
{
    public class PingPongClusterTests
    {
        public PingPongClusterTests()
        {
            _udpCommunication = A.Fake<IUdpCommunication>();
            _schedular = A.Fake<ISchedular>();
            _antiEntropy = A.Fake<IAntiEntropy>();
            _settings = A.Fake<IJsonConfigurator>();
            ConfigureSettings();
            throw new NotImplementedException();

            //A.CallTo(() => _antiEntropy.GetCommittedMessagesToSend(A<int>._, A<int>._)).Returns(new List<MessageReceivedEventArgs>());
            //A.CallTo(() => _antiEntropy.GetTentativeMessagesToSendForNonPrimaryBroker(A<int>._, A<int>._)).Returns(new List<MessageReceivedEventArgs>());
            //A.CallTo(() => _antiEntropy.GetTentativeMessagesToSendForNonPrimaryBroker(A<int>._, A<int>._)).Returns(new List<MessageReceivedEventArgs>());

            //_pingPong = new PingPong(_udpCommunication, _schedular, _antiEntropy, _settings);
        }

        private readonly PingPong _pingPong;
        private readonly IUdpCommunication _udpCommunication;
        private readonly ISchedular _schedular;
        private readonly IAntiEntropy _antiEntropy;
        private readonly IJsonConfigurator _settings;

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
                        Ipaddress = "foo"
                    }
                }
            });
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsNonPrimaryBroker()
        {
            throw new NotImplementedException();
            //ConfigureSettings(false);
            //var pingPongNonPrimaryBroker = new PingPong(_udpCommunication, _schedular, _antiEntropy, _settings);
            //var messageReceived = new MessageReceivedEventArgs
            //{
            //    Port = 0,
            //    IpAddress = "foo",
            //    Packet = new Packet
            //    {
            //        Data = JsonSerializer.Serialize(new AntiEntropyMessage
            //        {
            //            ClockSync = TimeSpan.FromSeconds(10),
            //            PrimaryNumber = 1,
            //            Committed = new List<MessageReceivedEventArgs>(),
            //            Tentative = new List<MessageReceivedEventArgs>()
            //        })
            //    }
            //};

            //pingPongNonPrimaryBroker.AntiEntropyMessageReceived(messageReceived);

            //pingPongNonPrimaryBroker.Clock.ElapsedTimeSpan.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(10));
            //A.CallTo(() => _antiEntropy.AntiEntropyNonPrimaryMessageReceived(A<AntiEntropyMessage>._)).MustHaveHappened();
            //A.CallTo(() => _udpCommunication.Send(A<Packet>._, "foo", 0)).MustHaveHappened();
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsrimaryBroker()
        {
            throw new NotImplementedException();

            //var messageReceived = new MessageReceivedEventArgs
            //{
            //    Port = 123,
            //    IpAddress = "foo",
            //    AdministrationMessage = new Message
            //    {
            //        Data = JsonSerializer.Serialize(new AntiEntropyMessage
            //        {
            //            PrimaryNumber = 2,
            //            AntiEntropyRound = 0,
            //            Tentative = new List<MessageReceivedEventArgs>()
            //        })
            //    }
            //};
            //_pingPong.AntiEntropyMessageReceived(messageReceived);
            //var i = 0;
            //A.CallTo(() => _antiEntropy.AntiEntropyAddTentativeMessages(A<AntiEntropyMessage>._, ref i, ref i)).MustHaveHappened();
        }

        [Fact]
        public void CanHandleReceivedAntiEntropyMessageAsrimaryBrokerAfterThreeRoundsOfAntiEntropy()
        {
            throw new NotImplementedException();

            //var messageReceived = new MessageReceivedEventArgs
            //{
            //    Port = 123,
            //    IpAddress = "foo",
            //    AdministrationMessage = new Message
            //    {
            //        Data = JsonSerializer.Serialize(new AntiEntropyMessage
            //        {
            //            PrimaryNumber = 2,
            //            AntiEntropyRound = 3,
            //            Tentative = new List<MessageReceivedEventArgs>()
            //        })
            //    }
            //};
            //_pingPong.AntiEntropyMessageReceived(messageReceived);
            //var i = 0;
            //A.CallTo(() => _antiEntropy.AntiEntropyCommitStableMessages(ref i, ref i)).MustHaveHappened();
        }
    }
}