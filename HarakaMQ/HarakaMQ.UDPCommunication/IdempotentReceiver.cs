using System;
using System.Collections.Generic;
using HarakaMQ.UDPCommunication.Interfaces;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UDPCommunication
{
    public class IdempotentReceiver : IIdempotentReceiver
    {
        private readonly Dictionary<Guid, bool> RecievedMessages;

        public IdempotentReceiver()
        {
            RecievedMessages = new Dictionary<Guid, bool>();
        }

        public bool VerifyPacket(Packet packet)
        {
            if (RecievedMessages.ContainsKey(packet.Id))
                return false;

            RecievedMessages.Add(packet.Id, true);
            return true;
        }
    }
}