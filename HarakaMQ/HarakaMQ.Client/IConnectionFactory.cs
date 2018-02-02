namespace HarakaMQ.Client
{
    internal interface IConnectionFactory
    {
        IConnection CreateConnection();
    }
}