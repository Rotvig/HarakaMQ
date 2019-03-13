using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarakaMQ.MessageBroker.Interfaces;
using HarakaMQ.MessageBroker.Models;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using MessagePack;

namespace HarakaMQ.MessageBroker.Utils
{
    public class Clock : IClock
    {
        private readonly TimeSpan _refDateTimeNow;
        private readonly IUdpCommunication _udpCommunication;
        private TimeSpan _offsetTimeSpan;
        private readonly Stopwatch _stopwatch;

        private TimeSpan T1; // Time when sync message is send from Master 
        private TimeSpan T2; // Time when sync message is received at Slave 
        private TimeSpan T3; // Time when delay request message is send from Slave
        private TimeSpan T4; // Time when delay request message is received at Master

        public Clock(IUdpCommunication udpCommunication)
        {
            _stopwatch = new Stopwatch();
            _udpCommunication = udpCommunication;
            _offsetTimeSpan = TimeSpan.Zero;
            _refDateTimeNow = DateTime.Now.ToUniversalTime().TimeOfDay;
            _stopwatch.Start();
        }

        public void StartTimeSync(List<MessageBrokerInformation> brokersToSync)
        {
            //Todo: send clock sync receivedMessage T1 to brokers
            foreach (var brokerInformation in brokersToSync)
                _udpCommunication.SendAdministrationMessage(new AdministrationMessage
                {
                    Type = MessageType.ClockSync,
                    Data = MessagePackSerializer.Serialize(new ClockSyncMessage
                    {
                        Type = SyncType.SyncMessage,
                        TimeStamp = ElapsedTimeSpan()
                    })
                }, brokerInformation.Ipaddress, brokerInformation.Port);
        }

        public TimeSpan ElapsedTimeSpan()
        {
            return _stopwatch.Elapsed + _refDateTimeNow + _offsetTimeSpan;
        }

        public void ClockSyncMessageReceived(MessageReceivedEventArgs receivedMessage)
        {
            var syncMessage = MessagePackSerializer.Deserialize<ClockSyncMessage>(receivedMessage.AdministrationMessage.Data);
            switch (syncMessage.Type)
            {
                case SyncType.SyncMessage:
                    T1 = syncMessage.TimeStamp;
                    T2 = ElapsedTimeSpan();
                    T3 = ElapsedTimeSpan();
                    SendClockSyncMessage(receivedMessage, ElapsedTimeSpan(), SyncType.DelayRequestMessage);
                    break;
                case SyncType.DelayRequestMessage:
                    SendClockSyncMessage(receivedMessage, ElapsedTimeSpan(), SyncType.DelayResponseMessage);
                    break;
                case SyncType.DelayResponseMessage:
                    T4 = syncMessage.TimeStamp;
                    CalculateOffset();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SendClockSyncMessage(MessageReceivedEventArgs receivedMessage, TimeSpan elapsedTimeSpan, SyncType type)
        {
            _udpCommunication.SendAdministrationMessage(new AdministrationMessage
            {
                Type = MessageType.ClockSync,
                Data = MessagePackSerializer.Serialize(new ClockSyncMessage
                {
                    TimeStamp = elapsedTimeSpan,
                    Type = type
                })
            }, receivedMessage.IpAddress, receivedMessage.Port);
        }

        private void CalculateOffset()
        {
            _offsetTimeSpan = ((T2 - T1) - (T4 - T3)) / 2;
        }
    }

    [MessagePackObject]
    public class ClockSyncMessage
    {
        [Key(0)]
        public TimeSpan TimeStamp { get; set; }

        [Key(1)]
        public SyncType Type { get; set; }
    }

    public enum SyncType
    {
        SyncMessage,
        DelayRequestMessage,
        DelayResponseMessage
    }
}