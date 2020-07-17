namespace ExactlyOnce.NServiceBus
{
    public class Message
    {
        public Message(string messageId, byte[] body)
        {
            MessageId = messageId;
            Body = body;
        }

        public string MessageId { get; }
        public byte[] Body { get; }
    }
}