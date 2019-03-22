using MessagePack;

namespace HarakaMQ.UDPCommunication.Models
{
    [MessagePackObject]
    public class Client
    {
        [IgnoreMember]
        public string Id => Host.IPAddress + Host.Port;

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
        public Host Host { get; set; }

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