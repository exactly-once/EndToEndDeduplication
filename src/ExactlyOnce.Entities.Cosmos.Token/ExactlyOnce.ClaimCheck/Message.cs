namespace ExactlyOnce.ClaimCheck
{
    public class Message
    {
        public Message(string id, byte[] body)
        {
            Id = id;
            Body = body;
        }

        public string Id { get; }
        public byte[] Body { get; }
    }
}