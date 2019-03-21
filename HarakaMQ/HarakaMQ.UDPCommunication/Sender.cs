﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using HarakaMQ.Shared;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication
{
    public class Sender : ISender, IDisposable
    {
        private readonly ISerializer _serialiser;
        private readonly Socket _socket;

        public Sender(IHarakaMQUDPConfiguration harakaMqudpConfiguration, ISerializer serialiser)
        {
            _serialiser = serialiser;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            if (harakaMqudpConfiguration.DisableIPv4Fragmentation)
                _socket.DontFragment = true;
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        public async Task SendMsg(SenderMessage msg, string ip, int port)
        {
            var broadcast = IPAddress.Parse(ip);
            var ep = new IPEndPoint(broadcast, port);

            await _socket.SendToAsync(new ArraySegment<byte>(_serialiser.Serialize(msg)), SocketFlags.None , ep);
        }
    }
}