using System;
using HarakaMQ.Client.Events;

namespace HarakaMQ.Client
{
    public interface IBasicConsumer
    {
        /// <summary>
        ///     Retrieve the <see cref="IModel" /> this consumer is associated with,
        ///     for use in acknowledging received messages, for instance.
        /// </summary>
        IModel Model { get; }

        event EventHandler<BasicDeliverEventArgs> Received;
        void MsgReceived(BasicDeliverEventArgs e);
    }
}