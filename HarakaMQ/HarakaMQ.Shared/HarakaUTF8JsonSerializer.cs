using Utf8Json;

namespace HarakaMQ.Shared
{
    public class HarakaUTF8JsonSerializer : ISerializer
    {
        public byte[] Serialize<T>(T content)
        {
            return JsonSerializer.Serialize<T>(content);
        }

        public T Deserialize<T>(byte[] content)
        {
            return JsonSerializer.Deserialize<T>(content);
        }
    }
}