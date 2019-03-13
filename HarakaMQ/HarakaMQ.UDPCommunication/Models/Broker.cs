namespace HarakaMQ.UDPCommunication.Models
{
    public class Broker
    {
        public string IpAdress { get; set; }
        public int Port { get; set; }
        public string Id => IpAdress + Port;
    }
}