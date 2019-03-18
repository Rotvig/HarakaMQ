using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;
using HarakaMQ.UDPCommunication.Utils;
using MessagePack;

namespace HarakaMQ.UDPCommunication
{
    public class Sender : ISender, IDisposable
    {
        private readonly Socket _socket;

        public Sender(IHarakaMQUDPConfiguration harakaMqudpConfiguration)
        {
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

            await _socket.SendToAsync(new ArraySegment<byte>(MessagePackSerializer.Serialize(msg)), SocketFlags.None , ep);
        }
    }
}