using System;
using System.Collections.Generic;
using System.Linq;
using HarakaMQ.Client.Events;
using HarakaMQ.UDPCommunication.Events;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.Client
{
    public class Model : IModel
    {
        private readonly IUdpCommunication _comm;
        private readonly List<Tuple<IBasicConsumer, string>> consumers;
        private int _listenPort;

        public Model(IUdpCommunication udpComm, int listenPort, string ip, int brokerPort)
        {
            _listenPort = listenPort;
            _comm = udpComm;
            consumers = new List<Tuple<IBasicConsumer, string>>();
            _comm.PublishPackage += OnMessageReceived;
            _comm.Listen(listenPort);
            _comm.SetBrokerInformation(ip, brokerPort);
        }

        public void BasicConsume(string queue, IBasicConsumer consumer)
        {
            var msg = new AdministrationMessage(MessageType.Subscribe, queue);
            _comm.SendAdministrationMessage(msg);
            consumers.Add(new Tuple<IBasicConsumer, string>(consumer, queue));
        }

        public void BasicPublish(string routingKey, byte[] body)
        {
            var msg = new Message(body);
            _comm.Send(msg, routingKey);
        }

        public void Dispose()
        {
            //Todo: Implement Dispose remvoe events and close down listeners/unsub from server
            //throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclare(string queue)
        {
            var msg = new AdministrationMessage(MessageType.QueueDeclare, queue);
            _comm.SendAdministrationMessage(msg);
            return new QueueDeclareOk(queue, 0, 0);
        }

        protected virtual void OnMessageReceived(object sender, PublishPacketReceivedEventArgs e)
        {
            if (!consumers.Any()) return;
            var consumer = consumers.Find(x => x.Item2 == e.Packet.Topic);
            if (consumer == null) return;

            for (var i = 0; i < e.Packet.Messages.Count; i++)
            {
                var args = new BasicDeliverEventArgs {Body = e.Packet.Messages[i]};
                consumer.Item1.MsgReceived(args);
            }
        }
    }
}