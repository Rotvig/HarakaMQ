﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace HarakaMQ.UDPCommunication.Utils
{
    internal static class Ext
    {
        internal static string GetIp4Address()
        {
            var ip4Address = string.Empty;

            foreach (
                var ipa in
                Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(ipa => ipa.AddressFamily == AddressFamily.InterNetwork))
            {
                ip4Address = ipa.ToString();
                break;
            }

            return ip4Address;
        }
    }
}