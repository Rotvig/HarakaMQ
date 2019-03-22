using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using HarakaMQ.DB;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.UDPCommunication
{
    public class GuranteedDelivery : IGuranteedDelivery, IDisposable
    {
        private readonly IHarakaDb _harakaDb;
        private readonly ISerializer _serializer;
        private readonly IIdempotentReceiver _idempotentReceiver;
        private readonly IReceiver _receiver;
        private readonly ISender _sender;
        private Task _queueConsumerThread;
        private bool _stopConsuming;

        public GuranteedDelivery(ISender sender, IReceiver receiver, IIdempotentReceiver idempotentReceiver, IHarakaDb harakaDb, ISerializer serializer)
        {
            _sender = sender;
            _receiver = receiver;
            _harakaDb = harakaDb;
            _serializer = serializer;
            _idempotentReceiver = idempotentReceiver;
        }

        public void Dispose()
        {
            _stopConsuming = true;
            _queueConsumerThread?.Wait();
            _queueConsumerThread?.Dispose();
        }

        public event EventHandler<ExtendedPacketInformation> MessageReceived;

        public void Listen(int port)
        {
            _receiver.ReceivedMessage += OnReceivedMessage;
            _receiver.StartListenerAsync(port);
            //Todo Fix startup sequence
            //StartupSequence();
        }

        public async Task Send(ExtendedPacketInformation msg)
        {
            var sendMsg = new SenderMessage
            {
                Body = _serializer.Serialize(msg.Packet),
                Type = msg.UdpMessageType
            };

            lock (_harakaDb.GetLock(Setup.OutgoingMessagesCS))
            {
                var messages = _harakaDb.GetObjects<ExtendedPacketInformation>(Setup.OutgoingMessagesCS);
                messages.Add(msg);
                _harakaDb.StoreObject(Setup.OutgoingMessagesCS, messages);
            }

            await _sender.SendMsg(sendMsg, msg.Host);
        }

        public void ReSend(Guid messageId)
        {
            var extendedPacketInformation = _harakaDb.TryGetObjects<ExtendedPacketInformation>(Setup.OutgoingMessagesCS).Find(x => x.Packet.Id == messageId);

            var sendMsg = new SenderMessage
            {
                Body = _serializer.Serialize(extendedPacketInformation.Packet),
                Type = extendedPacketInformation.UdpMessageType
            };
            _sender.SendMsg(sendMsg, extendedPacketInformation.Host);
        }

        public void ReSend(string clientId, int seqNo)
        {
            var extendedPacketInformation = _harakaDb.TryGetObjects<ExtendedPacketInformation>(Setup.OutgoingMessagesCS).Find(x => x.SenderClient == clientId && x.Packet.SeqNo == seqNo);

            var sendMsg = new SenderMessage
            {
                Body = _serializer.Serialize(extendedPacketInformation.Packet),
                Type = extendedPacketInformation.UdpMessageType
            };
            _sender.SendMsg(sendMsg, extendedPacketInformation.Host);
        }
        
        public void SendUdpMessage(UdpMessage msg, UdpMessageType type, Host host)
        {
            var sendMsg = new SenderMessage
            {
                Body = _serializer.Serialize(msg),
                Type = type
            };

            _sender.SendMsg(sendMsg, host);
        }

        public async Task RemoveMessagesFromSendQueueAsync(string clientId, int seqNo)
        {
            await Task.Run(() =>
            {
                lock (_harakaDb.GetLock(Setup.OutgoingMessagesCS))
                {
                    var messages = _harakaDb.GetObjects<ExtendedPacketInformation>(Setup.OutgoingMessagesCS);
                    messages.Remove(messages.First(x => x.SenderClient == clientId && x.Packet.SeqNo <= seqNo));
                    _harakaDb.StoreObject(Setup.OutgoingMessagesCS, messages);
                }
            });
        }

        public async Task RemoveMessageFromReceiveQueueAsync(Guid msgid)
        {
            await Task.Run(() =>
            {
                lock (_harakaDb.GetLock(Setup.IngoingMessagesCS))
                {
                    var messages = _harakaDb.GetObjects<ExtendedPacketInformation>(Setup.IngoingMessagesCS);
                    messages.Remove(messages.First(x => x.Id == msgid));
                    _harakaDb.StoreObject(Setup.IngoingMessagesCS, messages);
                }
            });
        }

        public void HandleExtendedMessageInformation(UdpReceiveResult result)
        {
            ExtendedPacketInformation extendedMsg;
            var deserializedUdpMessage = _serializer.Deserialize<SenderMessage>(result.Buffer);

            switch (deserializedUdpMessage.Type)
            {
                case UdpMessageType.DelayedAck:
                case UdpMessageType.Packet:
                    var message = _serializer.Deserialize<Packet>(deserializedUdpMessage.Body);
                    message.Size = deserializedUdpMessage.Body.Length;

                    if (_idempotentReceiver.VerifyPacket(message))
                        lock (_harakaDb.GetLock(Setup.IngoingMessagesCS))
                        {
                            extendedMsg = new ExtendedPacketInformation(message, deserializedUdpMessage.Type, result.RemoteEndPoint.Address.ToString());
                            {
                                var messages = _harakaDb.GetObjects<ExtendedPacketInformation>(Setup.IngoingMessagesCS);
                                messages.Add(extendedMsg);
                                _harakaDb.StoreObject(Setup.IngoingMessagesCS, messages);
                            }
                            OnMessageReceived(extendedMsg);
                        }
                    break;
                case UdpMessageType.ResendRequest:
                    var resendMessage = _serializer.Deserialize<UdpMessage>(deserializedUdpMessage.Body);
                    ReSend(result.RemoteEndPoint.Address.ToString() + resendMessage.ReturnPort, resendMessage.SeqNo);
                    break;
                case UdpMessageType.DelayedAckResponse:
                case UdpMessageType.GarbageCollect:
                    var udpMessage = _serializer.Deserialize<UdpMessage>(deserializedUdpMessage.Body);
                    extendedMsg = new ExtendedPacketInformation(udpMessage, deserializedUdpMessage.Type, result.RemoteEndPoint.Address.ToString());
                    OnMessageReceived(extendedMsg);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void OnReceivedMessage(object o, EventArgs f)
        {
            //Start consumer thread and consume recieved messages
            if (_queueConsumerThread == null || _queueConsumerThread.IsCompleted)
                _queueConsumerThread = Task.Factory.StartNew(() =>
                {
                    while (_receiver.QueueCount() > 0 || _stopConsuming)
                        HandleExtendedMessageInformation(_receiver.DequeueUdpResult());
                });
        }

        private void StartupSequence()
        {
            var ingoingMessageInformation = _harakaDb.TryGetObjects<ExtendedPacketInformation>(Setup.IngoingMessagesCS);
            if (ingoingMessageInformation.Any())
                foreach (var extMsg in ingoingMessageInformation)
                    OnMessageReceived(extMsg);

            var outMessageInformation = _harakaDb.TryGetObjects<ExtendedPacketInformation>(Setup.OutgoingMessagesCS);

            if (!outMessageInformation.Any()) return;
            {
                foreach (var extendedMsg in outMessageInformation)
                {
                    var sendMsg = new SenderMessage
                    {
                        Body = _serializer.Serialize(extendedMsg.Packet),
                        Type = extendedMsg.UdpMessageType
                    };
                    _sender.SendMsg(sendMsg, extendedMsg.Host);
                }
            }
        }

        protected virtual void OnMessageReceived(ExtendedPacketInformation e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }
}