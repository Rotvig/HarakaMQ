namespace HarakaMQ.Shared
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T content);
        T Deserialize<T>(byte[] content);
    }
}