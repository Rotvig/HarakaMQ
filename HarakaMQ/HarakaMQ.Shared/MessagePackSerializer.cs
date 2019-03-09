namespace HarakaMQ.Shared
{
    public class MessagePackSerializer : ISerializer
    {
        private readonly MessagePackSerializer _messagePackSerializer;

        public MessagePackSerializer(MessagePackSerializer messagePackSerializer)
        {
            _messagePackSerializer = messagePackSerializer;
        }

        public byte[] Serialize<T>(T content)
        {
            return _messagePackSerializer.Serialize(content);
        }

        public T Deserialize<T>(byte[] content)
        {
            return _messagePackSerializer.Deserialize<T>(content);
        }
    }
}