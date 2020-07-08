namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class HttpRequestRecord : SideEffectRecord
    {
        public string Id { get; set; }
        public string Url { get; set; }
    }
}