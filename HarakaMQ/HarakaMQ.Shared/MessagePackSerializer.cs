﻿namespace HarakaMQ.Shared
{
    public class MessagePackSerializer : ISerializer
    {
        public byte[] Serialize<T>(T content)
        {
            return MessagePack.MessagePackSerializer.Serialize(content);
        }

        public T Deserialize<T>(byte[] content)
        {
            return MessagePack.MessagePackSerializer.Deserialize<T>(content);
        }
    }
}