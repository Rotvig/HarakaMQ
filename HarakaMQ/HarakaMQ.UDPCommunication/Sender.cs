using System;
using System.Net;
using System.Net.Sockets;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using MessagePack;

namespace HarakaMQ.UDPCommunication
{
    public class Sender : ISender, IDisposable
    {
        private readonly Socket _socket;

        public Sender()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            if (Setup.DontFragment)
                _socket.DontFragment = true;
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        public void SendMsg(SenderMessage msg, string ip, int port)
        {
            var broadcast = IPAddress.Parse(ip);
            var ep = new IPEndPoint(broadcast, port);

            _socket.SendTo(MessagePackSerializer.Serialize(msg), ep);
        }
    }
}