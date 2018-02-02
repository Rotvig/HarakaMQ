using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]
    public class Client
    {
        public Client()
        {
        }

        public Client(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        [IgnoreMember]
        public string Id => Ip + Port;

        /// <summary>
        ///     Represent the last message send
        /// </summary>
        [Key(0)]
        public int OutgoingSeqNo { get; set; }

        /// <summary>
        ///     Represent the last message recieved in order
        /// </summary>
        [Key(1)]
        public int IngoingSeqNo { get; set; }

        [Key(2)]
        public string Ip { get; set; }

        [Key(3)]
        public int Port { get; set; }

        public void SetIngoingSeqNo(int number)
        {
            IngoingSeqNo = number;
        }

        public void SetOutgoingSeqNo(int number)
        {
            OutgoingSeqNo = number;
        }
    }
}