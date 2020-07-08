using System.Collections.Generic;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class OutgoingMessageRecord : SideEffectRecord
    {
        public string Id { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}