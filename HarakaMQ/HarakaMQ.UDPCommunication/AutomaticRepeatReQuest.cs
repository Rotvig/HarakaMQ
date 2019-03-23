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
using Microsoft.Extensions.Logging;

namespace HarakaMQ.UDPCommunication
{
    public class AutomaticRepeatReQuest : IAutomaticRepeatReQuest
    {
        private readonly IGuranteedDelivery _guranteedDelivery;
        private readonly IHarakaDb _harakaDb;
        private readonly IHarakaMQUDPConfiguration _harakaMqudpConfiguration;
        private readonly ILogger<AutomaticRepeatReQuest> _logger;
        private readonly Dictionary<string, ConcurrentQueue<Tuple<string, Message>>> _messagesToPacket;
        private readonly ISchedular _schedular;
        private readonly Dictionary<string, SortedList<int, ExtendedPacketInformation>> _sortedReceivedMessages;

        public AutomaticRepeatReQuest(IGuranteedDelivery guranteedDelivery, ISchedular schedular, IHarakaDb harakaDb, IHarakaMQUDPConfiguration harakaMqudpConfiguration, ILogger<AutomaticRepeatReQuest> logger)
        {
            _guranteedDelivery = guranteedDelivery;
            _schedular = schedular;
            _harakaDb = harakaDb;
            _harakaMqudpConfiguration = harakaMqudpConfiguration;
            _logger = logger;
            _sortedReceivedMessages = new Dictionary<string, SortedList<int, ExtendedPacketInformation>>();
            _messagesToPacket = new Dictionary<string, ConcurrentQueue<Tuple<string, Message>>>();
            logger.LogInformation("Initialized AutomaticRepeatRequest");
        }

        public event EventHandler<EventArgs> MessageNotSend;
        public event EventHandler<MessageReceivedEventArgs> QueueDeclare;
        public event EventHandler<PublishPacketReceivedEventArgs> PublishPackage;
        public event EventHandler<MessageReceivedEventArgs> Subscribe;
        public event EventHandler<MessageReceivedEventArgs> Unsubscribe;
        public event EventHandler<MessageReceivedEventArgs> AntiEntropyMessage;
        public event EventHandler<MessageReceivedEventArgs> ClockSyncMessage;

        public async Task Send(Message msg, string topic, Host host)
        {
            if (_messagesToPacket.ContainsKey(host.IPAddress + ":" + host.Port))
            {
                _messagesToPacket[host.IPAddress + ":" + host.Port].Enqueue(new Tuple<string, Message>(topic, msg));
            }
            else
            {
                var queue = new ConcurrentQueue<Tuple<string, Message>>();
                queue.Enqueue(new Tuple<string, Message>(topic, msg));

                if(!_messagesToPacket.ContainsKey(host.IPAddress + ":" + host.Port))
                {
                    _messagesToPacket.Add(host.IPAddress + ":" + host.Port, queue);
                }
            }

            await MessageReadyToSend();
        }

        public void SendAdministrationMessage(AdministrationMessage msg, Host host)
        {
            var client = GetClient(host.IPAddress, host.Port);
            var package = new Packet(_harakaMqudpConfiguration.ListenPort) {SeqNo = GetOutGoingSeqNo(host.IPAddress, host.Port), Type = PacketType.AdminitrationMessages, AdministrationMessage = msg};
            _guranteedDelivery.Send(new ExtendedPacketInformation(package, UdpMessageType.Packet, client.Host.IPAddress, client.Host.Port));

            if (!_harakaMqudpConfiguration.DisableDelayedAcknowledgeForClientWithIds.Contains(client.Id))
                _schedular.TryScheduleDelayedAck(_harakaMqudpConfiguration.DelayedAcknowledgeWaitTimeInMiliseconds, client.Id, SendDelayedAck);
        }

        public async Task SendPacket(Packet packet, Host host)
        {
            var client = GetClient(host.IPAddress, host.Port);
            packet.ReturnPort = _harakaMqudpConfiguration.ListenPort;
            packet.SeqNo = GetOutGoingSeqNo(host.IPAddress, host.Port);
           await _guranteedDelivery.Send(new ExtendedPacketInformation(packet, UdpMessageType.Packet, client.Host.IPAddress, client.Host.Port));

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

                await _guranteedDelivery.Send(new ExtendedPacketInformation(package, UdpMessageType.Packet, client.Host));

                if (!_harakaMqudpConfiguration.DisableDelayedAcknowledgeForClientWithIds.Contains(client.Id))
                    _schedular.TryScheduleDelayedAck(_harakaMqudpConfiguration.DelayedAcknowledgeWaitTimeInMiliseconds, client.Id, SendDelayedAck);
            }
        }

        private void SendDelayedAck(string clientId)
        {
            var package = new Packet(_harakaMqudpConfiguration.ListenPort) {SeqNo = UpdateAndGetOutGoingSeqNo(clientId, out var client)};

            _guranteedDelivery.Send(new ExtendedPacketInformation(package, UdpMessageType.DelayedAck, client.Host.IPAddress, client.Host.Port));
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

                    var client = GetClient(receivedPacket.Host.IPAddress, receivedPacket.Host.Port);
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
                        IpAddress = extendedPacketInformation.Host.IPAddress,
                        Port = extendedPacketInformation.Host.Port,
                        Type = extendedPacketInformation.UdpMessageType
                    });
                else
                    PublishPacketToApplicationLayer(new PublishPacketReceivedEventArgs {Packet = extendedPacketInformation.Packet, Port = extendedPacketInformation.Packet.ReturnPort, IpAddress = extendedPacketInformation.Host.IPAddress});

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
            var client = GetClient(receivedPacket.Host.IPAddress, receivedPacket.Packet.ReturnPort);
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
                _schedular.ScheduleRecurringResend(100, receivedPacket.Host.IPAddress, receivedPacket.Host.Port, client.IngoingSeqNo + 1, SendResendRequest);
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
                    _schedular.ScheduleRecurringResend(100, receivedPacket.Host.IPAddress, receivedPacket.Host.Port, receivedPacket.Packet.SeqNo + i + 1, SendResendRequest);
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

            _guranteedDelivery.SendUdpMessage(msg, UdpMessageType.ResendRequest, client.Host);
            //Debug.WriteLine("ResendScheduled: " + seqNo);
        }

        private void SendGarbageCollectMessage(ExtendedPacketInformation extendedPacketInformation)
        {
            var msg = new UdpMessage
            {
                SeqNo = extendedPacketInformation.Packet.SeqNo,
                ReturnPort = _harakaMqudpConfiguration.ListenPort
            };

            _guranteedDelivery.SendUdpMessage(msg, UdpMessageType.GarbageCollect, extendedPacketInformation.Host);
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

            _guranteedDelivery.SendUdpMessage(msg, UdpMessageType.DelayedAckResponse, extendedPacketInformation.Host);
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
                client = clients.Find(x => x.Host.IPAddress == ip && x.Host.Port == port);
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

                client = new Client() {Host = new Host{IPAddress = ip, Port = port}};
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