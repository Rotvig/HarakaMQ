using System;

namespace HarakaMQ.Client.Events
{
    public class BasicDeliverEventArgs : EventArgs
    {
        ///<summary>The message body.</summary>
        public byte[] Body { get; set; }
    }
}