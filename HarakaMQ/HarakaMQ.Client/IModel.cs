using System;

namespace HarakaMQ.Client
{
    public interface IModel : IDisposable
    {
        QueueDeclareOk QueueDeclare(string queue);
        void BasicPublish(string routingKey, byte[] body);
        void BasicConsume(string queue, IBasicConsumer consumer);
    }
}