using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarakaMQ.DB;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.UDPCommunication
{
    public class AutomaticRepeatReQuest : IAutomaticRepeatReQuest
    {
        private static volatile bool _taskAlreadyRunning;
        private readonly IGuranteedDelivery _guranteedDelivery;
        private readonly IHarakaDb _harakaDb;
        private readonly HarakaMQUDPConfiguration _harakaMqudpConfiguration;
        private readonly Dictionary<string, ConcurrentQueue<Tuple<string, Message>>> _messagesToPacket;
        private readonly ISchedular _schedular;
        private readonly Dictionary<string, SortedList<int, ExtendedPacketInformation>> _sortedReceivedMessages;

        public AutomaticRepeatReQuest(IGuranteedDelivery guranteedDelivery, ISchedular schedular, IHarakaDb harakaDb, HarakaMQUDPConfiguration harakaMqudpConfiguration)
        {
            _guranteedDelivery = guranteedDelivery;
            _schedular = schedular;
            _harakaDb = harakaDb;
            _harakaMqudpConfiguration = harakaMqudpConfiguration;
            _sortedReceivedMessages = new Dictionary<string, SortedList<int, ExtendedPacketInformation>>();
            _messagesToPacket = new Dictionary<string, ConcurrentQueue<Tuple<string, Message>>>();
        }

        public event EventHandler<EventArgs> MessageNotSend;
        public event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        public event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        public event EventHandler<MessageReceivedEventArgs> Subscribe;
        public event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        public event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        public event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;

        public async Task Send(Message msg, string topic, Broker broker)
        {
            if (_messagesToPacket.ContainsKey(broker.IpAdress + ":" + broker.Port))
            {
                _messagesToPacket[broker.IpAdress + ":" + broker.Port].Enqueue(new Tuple<string, Message>(topic, msg));
            }
            else
            {
                var queue = new ConcurrentQueue<Tuple<string, Message>>();
                queue.Enqueue(new Tuple<string, Message>(topic, msg));

                if(!_messagesToPacket.ContainsKey(broker.IpAdress + ":" + broker.Port))
                {
                    _messagesToPacket.Add(broker.IpAdress + ":" + broker.Port, queue);
                }
            }

            if (!_taskAlreadyRunning)
            {
                _taskAlreadyRunning = true;
                await MessageReadyToSend();
            }
        }

        public void SendAdministrationMessage(AdministrationMessage msg, Broker broker)
        {
            var client = GetClient(broker.IpAdress, broker.Port);
            var package = new Packet(_harakaMqudpConfiguration.ListenPort) {SeqNo = GetOutGoingSeqNo(broker.IpAdress, broker.Port), Type = PacketType.AdminitrationMessages, AdministrationMessage = msg};
            _guranteedDelivery.Send(new ExtendedPacketInformation(package, UdpMessageType.Packet, client.Ip, client.Port));

            if (!_harakaMqudpConfiguration.DisableDelayedAcknowledgeForClientWithIds.Contains(client.Id))
                _schedular.TryScheduleDelayedAck(_harakaMqudpConfiguration.DelayedAcknowledgeWaitTimeInMiliseconds, client.Id, SendDelayedAck);
        }

        public async Task SendPacket(Packet packet, Broker broker)
        {
            var client = GetClient(broker.IpAdress, broker.Port);
            packet.ReturnPort = _harakaMqudpConfiguration.ListenPort;
            packet.SeqNo = GetOutGoingSeqNo(broker.IpAdress, broker.Port);
           await _guranteedDelivery.Send(new ExtendedPacketInformation(packet, UdpMessageType.Packet, client.Ip, client.Port));

            if (!_harakaMqudpConfiguration.DisableDelayedAcknowledgeForClientWithIds.Contains(client.Id))
                _schedular.TryScheduleDelayedAck(_harakaMqudpConfiguration.DelayedAcknowledgeWaitTimeInMiliseconds, client.Id, SendDelayedAck);
        }

        public void Listen(int port)
        {
            _guranteedDelivery.MessageReceived += OnMessageReceived;
            _guranteedDelivery.Listen(port);
        }

        private async Task MessageReadyToSend()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5));
            await SendMessages();
        }

        private async Task SendMessages()
        {
            foreach (var messagesPerClient in _messagesToPacket)
                if (messagesPerClient.Value.Count > 0)
                    await SendPacket(messagesPerClient.Key, messagesPerClient.Value);
        }

        private async Task SendPacket(string clientId, ConcurrentQueue<Tuple<string, Message>> messagesToPackage)
        {
            var ipAndPort = clientId.Split(':');
            var client = GetClient(ipAndPort.First(), int.Parse(ipAndPort.Last()));
            var firstPriorityMessagesToSend = new List<Tuple<string, Message>>();
            while (messagesToPackage.Any())
            {
                string currentTopic = null;
                var numberOfBytes = 0;
                var messagesToSend = new List<byte[]>();

                while (messagesToPackage.Any() || firstPriorityMessagesToSend.Any())
                {
                    Tuple<string, Message> message;
                    if (firstPriorityMessagesToSend.Any())
                    {
                        message = firstPriorityMessagesToSend.First();
                        firstPriorityMessagesToSend.Remove(message);
                    }
                    else
                    {
                        messagesToPackage.TryDequeue(out message);
                    }

                    if (currentTopic == null)
                        currentTopic = message.Item1;

                    if (currentTopic != message.Item1)
                    {
                        firstPriorityMessagesToSend.Add(message);
                        break;
                    }

                    if (numberOfBytes + message.Item2.Length <= Setup.MaxPayloadSize)
                    {
                        messagesToSend.Add(message.Item2.GetMessage());
                        numberOfBytes += message.Item2.Length;
                    }
                    else
                    {
                        firstPriorityMessagesToSend.Add(message);
                        break;
                    }
                }

                var package = new Packet(_harakaMqudpConfiguration.ListenPort, messagesToSend) {SeqNo = GetOutGoingSeqNo(ipAndPort.First(), int.Parse(ipAndPort.Last())), Type = PacketType.Messages, Topic = currentTopic};

                await _guranteedDelivery.Send(new ExtendedPacketInformation(package, UdpMessageType.Packet, client.Ip, client.Port));

                if (!_harakaMqudpConfiguration.DisableDelayedAcknowledgeForClientWithIds.Contains(client.Id))
                    _schedular.TryScheduleDelayedAck(_harakaMqudpConfiguration.DelayedAcknowledgeWaitTimeInMiliseconds, client.Id, SendDelayedAck);
            }
            _taskAlreadyRunning = false;
        }

        private void SendDelayedAck(string clientId)
        {
            var package = new Packet(_harakaMqudpConfiguration.ListenPort) {SeqNo = UpdateAndGetOutGoingSeqNo(clientId, out var client)};

            _guranteedDelivery.Send(new ExtendedPacketInformation(package, UdpMessageType.DelayedAck, client.Ip, client.Port));
            _schedular.ScheduleRecurringResend(_harakaMqudpConfiguration.DelayedAcknowledgeWaitTimeInMiliseconds, package.Id, _guranteedDelivery.ReSend);
            //Debug.WriteLine("ClientId :" + Setup.ClientId + " Delayed Ack send at msg num: " + msg.SeqNo);
        }

        private async void OnMessageReceived(object sender, ExtendedPacketInformation e)
        {
            await HandleRecievedMessage(e);
        }

        public async Task HandleRecievedMessage(ExtendedPacketInformation receivedPacket)
        {
            switch (receivedPacket.UdpMessageType)
            {
                case UdpMessageType.Packet:
                    await HandleMessage(receivedPacket);
                    break;
                case UdpMessageType.DelayedAckResponse:
                    _schedular.CancelTask(receivedPacket.UdpMessage.MessageId.Value);
                    if (receivedPacket.UdpMessage.InOrder)
                        await _guranteedDelivery.RemoveMessagesFromSendQueueAsync(receivedPacket.SenderClient, receivedPacket.UdpMessage.SeqNo);
                    break;
                case UdpMessageType.DelayedAck:

                    var client = GetClient(receivedPacket.Ip, receivedPacket.Port);
                    //Debug.WriteLine("Received Delayed ack: " + receivedPacket.Packet.Id);
                    if (client.IngoingSeqNo + 1 == receivedPacket.Packet.SeqNo)
                    {
                        //Inorder
                        SendDelayedAckResponse(receivedPacket, true);
                        UpdateIngoingSequence(receivedPacket.SenderClient, receivedPacket.Packet.SeqNo);
                        await _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(receivedPacket.Id);
                    }
                    else
                    {
                        //OutOfOrder
                        SendDelayedAckResponse(receivedPacket, false);
                        await HandleMessage(receivedPacket);
                    }
                    break;
                case UdpMessageType.GarbageCollect:
                    await _guranteedDelivery.RemoveMessagesFromSendQueueAsync(receivedPacket.SenderClient, receivedPacket.UdpMessage.SeqNo);
                    break;
                default:
                    throw new ArgumentException("Couldnt handle message");
            }
        }


        private async Task HandleMessage(ExtendedPacketInformation receivedPacket)
        {
            //Cancel any recurring resendrequests
            //Todo: Cancel task based on clintid and seqno
            _schedular.CancelTask(receivedPacket.Packet.Id, receivedPacket.SenderClient, receivedPacket.Packet.SeqNo);
            //Debug.WriteLine("Packet received SeqNo: " + receivedPacket.Packet.SeqNo);


            //Check Order, dont publish event if its out of order
            var packetsToPublish = GetPacketsToPublish(receivedPacket);

            //PublishPackage all events in order
            foreach (var extendedPacketInformation in packetsToPublish)
            {
                // Send ack after a set number of messages
                if (extendedPacketInformation.Packet.SeqNo % _harakaMqudpConfiguration.AcknowledgeMessageAfterNumberOfMessages == 0)
                    SendGarbageCollectMessage(extendedPacketInformation);

                if (extendedPacketInformation.UdpMessageType == UdpMessageType.DelayedAck)
                {
                    //Send garbagecollect mesage
                    SendGarbageCollectMessage(extendedPacketInformation);
                    await RemoveFromReceiveQueueAndUpdateSeqNo(receivedPacket, extendedPacketInformation);
                    //Dont publish the delayedAck
                    continue;
                }
                if (extendedPacketInformation.Packet.Type == PacketType.AdminitrationMessages)
                    PublishEventToApplicationLayer(new MessageReceivedEventArgs
                    {
                        AdministrationMessage = extendedPacketInformation.Packet.AdministrationMessage,
                        IpAddress = extendedPacketInformation.Ip,
                        Port = extendedPacketInformation.Port,
                        Type = extendedPacketInformation.UdpMessageType
                    });
                else
                    PublishPacketToApplicationLayer(new PublishPacketReceivedEventArgs {Packet = extendedPacketInformation.Packet, Port = extendedPacketInformation.Packet.ReturnPort, IpAddress = extendedPacketInformation.Ip});

                await RemoveFromReceiveQueueAndUpdateSeqNo(receivedPacket, extendedPacketInformation);
            }
        }

        private async Task RemoveFromReceiveQueueAndUpdateSeqNo(ExtendedPacketInformation receivedPacket, ExtendedPacketInformation extendedPacketInformation)
        {
            //Remove Packet from Recieve Queue
            await _guranteedDelivery.RemoveMessageFromReceiveQueueAsync(extendedPacketInformation.Id);
            //Update Client
            UpdateIngoingSequence(receivedPacket.SenderClient, extendedPacketInformation.Packet.SeqNo);
        }

        private IEnumerable<ExtendedPacketInformation> GetPacketsToPublish(ExtendedPacketInformation receivedPacket)
        {
            var packetsToPublish = new Queue<ExtendedPacketInformation>();
            var client = GetClient(receivedPacket.Ip, receivedPacket.Packet.ReturnPort);
            //Add Client to dictionary if it does not exist
            if (!_sortedReceivedMessages.ContainsKey(client.Id))
                _sortedReceivedMessages.Add(client.Id, new SortedList<int, ExtendedPacketInformation>());

            if (client.IngoingSeqNo + 1 == receivedPacket.Packet.SeqNo) // +1 because ingoingSeqNo represents the last recieved
            {
                GetSortedPacketsInOrder(receivedPacket, packetsToPublish);
            }
            else
            {
                _sortedReceivedMessages[receivedPacket.SenderClient].Add(receivedPacket.Packet.SeqNo, receivedPacket);
                _schedular.ScheduleRecurringResend(100, receivedPacket.Ip, receivedPacket.Port, client.IngoingSeqNo + 1, SendResendRequest);
            }
            return packetsToPublish;
        }

        private void GetSortedPacketsInOrder(ExtendedPacketInformation receivedPacket, Queue<ExtendedPacketInformation> packetsToPublish)
        {
            packetsToPublish.Enqueue(receivedPacket);
            var count = _sortedReceivedMessages[receivedPacket.SenderClient].Count;
            for (var i = 0; i < count; i++)
                //Find The next message inorder
                // It is always Keys[0] because it has the lowest seqNo always, it will adjsut when elements on the list gets removed.
                // It is receivedPacket.Packet.SeqNo + i + 1. Example: ingoing message is 4, now we have to find 5 = 4 + 0 + 1 = 5
                if (_sortedReceivedMessages[receivedPacket.SenderClient].Keys[0] == receivedPacket.Packet.SeqNo + i + 1)
                {
                    packetsToPublish.Enqueue(_sortedReceivedMessages[receivedPacket.SenderClient].Values[0]);
                    //Remove the message from the sorted list
                    _sortedReceivedMessages[receivedPacket.SenderClient].Remove(_sortedReceivedMessages[receivedPacket.SenderClient].Keys[0]);
                }
                //Messages are not in order anymore then stop iterating
                else
                {
                    _schedular.ScheduleRecurringResend(100, receivedPacket.Ip, receivedPacket.Port, receivedPacket.Packet.SeqNo + i + 1, SendResendRequest);
                    break;
                }
        }

        private void SendResendRequest(string ip, int port, int seqNo)
        {
            var msg = new UdpMessage
            {
                SeqNo = seqNo,
                ReturnPort = _harakaMqudpConfiguration.ListenPort
            };

            var client = GetClient(ip, port);

            _guranteedDelivery.SendUdpMessage(msg, UdpMessageType.ResendRequest, client.Ip, client.Port);
            //Debug.WriteLine("ResendScheduled: " + seqNo);
        }

        private void SendGarbageCollectMessage(ExtendedPacketInformation extendedPacketInformation)
        {
            var msg = new UdpMessage
            {
                SeqNo = extendedPacketInformation.Packet.SeqNo,
                ReturnPort = _harakaMqudpConfiguration.ListenPort
            };

            _guranteedDelivery.SendUdpMessage(msg, UdpMessageType.GarbageCollect, extendedPacketInformation.Ip, extendedPacketInformation.Port);
        }

        private void SendDelayedAckResponse(ExtendedPacketInformation extendedPacketInformation, bool inOrder)
        {
            var msg = new UdpMessage
            {
                SeqNo = extendedPacketInformation.Packet.SeqNo,
                ReturnPort = _harakaMqudpConfiguration.ListenPort,
                MessageId = extendedPacketInformation.Packet.Id,
                InOrder = inOrder
            };

            _guranteedDelivery.SendUdpMessage(msg, UdpMessageType.DelayedAckResponse, extendedPacketInformation.Ip, extendedPacketInformation.Port);
        }

        private void UpdateIngoingSequence(string clientId, int msgSeqNo)
        {
            lock (_harakaDb.GetLock(Setup.ClientsCS))
            {
                var clients = _harakaDb.GetObjects<Client>(Setup.ClientsCS);
                var client = clients.Find(x => x.Id == clientId);
                client.SetIngoingSeqNo(msgSeqNo);
                _harakaDb.StoreObject(Setup.ClientsCS, clients);
            }
        }

        private int GetOutGoingSeqNo(string ip, int port)
        {
            Client client;
            lock (_harakaDb.GetLock(Setup.ClientsCS))
            {
                var clients = _harakaDb.GetObjects<Client>(Setup.ClientsCS);
                client = clients.Find(x => x.Ip == ip && x.Port == port);
                client.SetOutgoingSeqNo(client.OutgoingSeqNo + 1);
                _harakaDb.StoreObject(Setup.ClientsCS, clients);
            }
            return client.OutgoingSeqNo;
        }

        private int UpdateAndGetOutGoingSeqNo(string clientId, out Client client)
        {
            lock (_harakaDb.GetLock(Setup.ClientsCS))
            {
                var clients = _harakaDb.GetObjects<Client>(Setup.ClientsCS);
                client = clients.Find(x => x.Id == clientId);
                client.SetOutgoingSeqNo(client.OutgoingSeqNo + 1);
                _harakaDb.StoreObject(Setup.ClientsCS, clients);
            }
            return client.OutgoingSeqNo;
        }

        /// <summary>
        ///     Gets or create client if it dont exist
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private Client GetClient(string ip, int port)
        {
            Client client;
            lock (_harakaDb.GetLock(Setup.ClientsCS))
            {
                var clients = _harakaDb.GetObjects<Client>(Setup.ClientsCS);
                client = clients.Find(x => x.Id == ip + port);

                if (client != null)
                    return client;

                client = new Client(ip, port);
                clients.Add(client);
                _harakaDb.StoreObject(Setup.ClientsCS, clients);
            }
            return client;
        }

        private void PublishPacketToApplicationLayer(PublishPacketReceivedEventArgs e)
        {
            PublishPackage?.Invoke(this, e);
        }

        private void PublishEventToApplicationLayer(MessageReceivedEventArgs e)
        {
            switch (e.AdministrationMessage.Type)
            {
                case MessageType.QueueDeclare:
                    OnQueueDeclare(e);
                    break;
                case MessageType.Subscribe:
                    OnSubscribe(e);
                    break;
                case MessageType.Unsubscribe:
                    OnUnsubscribe(e);
                    break;
                case MessageType.AntiEntropy:
                    OnAntiEntropy(e);
                    break;
                case MessageType.ClockSync:
                    OnClockSyncMessage(e);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual void OnSubscribe(MessageReceivedEventArgs e)
        {
            Subscribe?.Invoke(this, e);
        }

        protected virtual void OnQueueDeclare(MessageReceivedEventArgs e)
        {
            QueueDeclare?.Invoke(this, e);
        }

        protected virtual void OnMessageNotSend(EventArgs e)
        {
            MessageNotSend?.Invoke(this, e);
        }

        protected virtual void OnUnsubscribe(MessageReceivedEventArgs e)
        {
            Unsubscribe?.Invoke(this, e);
        }

        private void OnAntiEntropy(MessageReceivedEventArgs e)
        {
            AntiEntropyMessage?.Invoke(this, e);
        }

        private void OnClockSyncMessage(MessageReceivedEventArgs e)
        {
            ClockSyncMessage?.Invoke(this, e);
        }
    }
}