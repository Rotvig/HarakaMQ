using System.Net;
using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]

    public class Host
    {
        [Key(0)]
        public string IPAddress { get; set; }
        [Key(1)]
        public int Port { get; set; }
        [Key(2)]
        public string Id => IPAddress + Port;
        [Key(3)]
        public IPEndPoint IpEndPoint => new IPEndPoint(System.Net.IPAddress.Parse(IPAddress), Port);
    }
}