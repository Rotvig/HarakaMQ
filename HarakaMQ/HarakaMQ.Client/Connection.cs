using System;
using HarakaMQ.UDPCommunication;
using HarakaMQ.UDPCommunication.Interfaces;

namespace HarakaMQ.Client
{
    public class Connection : IConnection
    {
        private readonly int _brokerPort;
        private readonly string _ip;
        private readonly int _listenPort;
        private readonly IUdpCommunication _udpconn;

        public Connection(string ip, int brokerPort, int listenPort, bool dontFragment = false)
        {
            _udpconn = new UdpCommunication();
            _udpconn.SetUpUdpComponent(10, 500, dontFragment);
            _listenPort = listenPort;
            _ip = ip;
            _brokerPort = brokerPort;
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
            return new Model(_udpconn, _listenPort, _ip, _brokerPort);
        }

        public void Dispose()
        {
            //Todo: Implement Dispose remvoe events and close down listeners/unsub from server
            //throw new NotImplementedException();
        }
    }
}