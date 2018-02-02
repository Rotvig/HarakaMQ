namespace HarakaMQ.Client
{
    /// <summary>
    ///     Represents Queue info.
    /// </summary>
    public class QueueDeclareOk
    {
        /// <summary>
        ///     Creates a new instance of the <see cref="QueueDeclareOk" />.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageCount">Packet count.</param>
        /// <param name="consumerCount">Consumer count.</param>
        public QueueDeclareOk(string queueName, int messageCount, int consumerCount)
        {
            QueueName = queueName;
            MessageCount = messageCount;
            ConsumerCount = consumerCount;
        }

        /// <summary>
        ///     Consumer count.
        /// </summary>
        public int ConsumerCount { get; }

        /// <summary>
        ///     Packet count.
        /// </summary>
        public int MessageCount { get; }

        /// <summary>
        ///     Queue name.
        /// </summary>
        public string QueueName { get; }

        public static implicit operator string(QueueDeclareOk declareOk)
        {
            return declareOk.QueueName;
        }
    }
}