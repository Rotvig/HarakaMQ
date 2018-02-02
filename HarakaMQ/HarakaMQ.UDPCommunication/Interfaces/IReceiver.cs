using System;
using System.Net.Sockets;

namespace HarakaMQ.UDPCommunication.Interfaces
{
    public interface IReceiver
    {
        void StartListenerAsync(int port);
        event EventHandler ReceivedMessage;
        UdpReceiveResult DequeueUdpResult();
        int QueueCount();

        void StopListening();
    }
}