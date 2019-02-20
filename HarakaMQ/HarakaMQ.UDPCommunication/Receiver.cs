using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using HarakaMQ.UDPCommunication.Interfaces;

namespace HarakaMQ.UDPCommunication
{
    public class Receiver : IReceiver
    {
        private readonly ConcurrentQueue<UdpReceiveResult> Queue;
        private bool _stopListening;

        public Receiver()
        {
            Queue = new ConcurrentQueue<UdpReceiveResult>();
        }

        public event EventHandler ReceivedMessage;

        public async void StartListenerAsync(int port)
        {
            _stopListening = false;
            using (var udpClient = new UdpClient(port))
            {
                while (_stopListening != true)
                {
                    EnqueueUdpResult(await udpClient.ReceiveAsync());
                }
            }
        }

        public UdpReceiveResult DequeueUdpResult()
        {
            Queue.TryDequeue(out var result);
            return result;
        }

        public int QueueCount()
        {
            return Queue.Count;
        }

        public void StopListening()
        {
            _stopListening = true;
        }

        protected virtual void OnMessageReceived()
        {
            ReceivedMessage?.Invoke(this, EventArgs.Empty);
        }

        private void EnqueueUdpResult(UdpReceiveResult receivedResults)
        {
            Queue.Enqueue(receivedResults);
            OnMessageReceived();
        }
    }
}