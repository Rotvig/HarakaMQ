using System;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Utils;

namespace HarakaMQ.Client
{
    public class Connection : IConnection
    {
        private readonly int _listenPort;
        private readonly IUdpCommunication _udpconn;
        private IHarakaMQUDPConfiguration _harakaUDPConfiguration;

        public Connection(IHarakaMQUDPConfiguration harakaMqudpConfiguration = null)
        {
            _harakaUDPConfiguration = harakaMqudpConfiguration ?? new DefaultHarakaMQUDPConfiguration();
            _udpconn = new UdpCommunication();
            _udpconn.Listen(harakaMqudpConfiguration);
            _listenPort = harakaMqudpConfiguration.ListenPort;
        }

        int NetworkConnection.Port => _listenPort;

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public IModel CreateModel()
        {
            return new Model(_udpconn, _harakaUDPConfiguration);
        }

        public void Dispose()
        {
            //Todo: Implement Dispose remvoe events and close down listeners/unsub from brokers
            //throw new NotImplementedException();
        }
    }
}