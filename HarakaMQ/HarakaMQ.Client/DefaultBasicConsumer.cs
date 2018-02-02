using System;
using HarakaMQ.Client.Events;

namespace HarakaMQ.Client
{
    public class DefaultBasicConsumer : IBasicConsumer
    {
        public DefaultBasicConsumer(IModel model)
        {
            Model = model;
        }

        public event EventHandler<BasicDeliverEventArgs> Received;

        public void MsgReceived(BasicDeliverEventArgs e)
        {
            OnReceived(e);
        }

        public IModel Model { get; }

        protected virtual void OnReceived(BasicDeliverEventArgs e)
        {
            var handler = Received;
            handler?.Invoke(this, e);
        }
    }
}