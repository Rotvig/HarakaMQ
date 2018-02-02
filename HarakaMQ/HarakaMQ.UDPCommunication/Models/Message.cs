namespace HarakaMQ.UDPCommunication.Models
{
    public class Message
    {
        /// <summary>
        ///     Important ! Remember to assign the message an GUID as ID
        /// </summary>
        public Message()
        {
        }

        public Message(byte[] data)
        {
            Data = data;
        }

        public byte[] Data;

        public int Length => Data.Length;

        public byte[] GetMessage()
        {
            return Data;
        }
    }
}