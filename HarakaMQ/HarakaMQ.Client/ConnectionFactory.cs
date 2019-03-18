using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.Client
{
    public class ConnectionFactory : IConnectionFactory
    {
        public IConnection CreateConnection(IHarakaMQUDPConfiguration harakaMqudpConfiguration = null)
        {
            return new Connection(harakaMqudpConfiguration);
        }
    }
}