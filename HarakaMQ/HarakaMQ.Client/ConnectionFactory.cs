using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.Client
{
    public class ConnectionFactory : IConnectionFactory
    {
        public string HostName { get; set; } = "localhost";

        /// <summary>
        ///     indicates the default for the protocol should be used.
        /// </summary>
        public int Port { get; set; } = 11457;

        public int ListenPort { get; set; }
        public bool DontFragment { get; set; } = false;

        public IConnection CreateConnection(IHarakaMQUDPConfiguration harakaMqudpConfiguration = null)
        {
            return new Connection(harakaMqudpConfiguration);
        }
    }
}