using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.Client
{
    internal interface IConnectionFactory
    {
        IConnection CreateConnection(IHarakaMQUDPConfiguration harakaMqudpConfiguration);
    }
}