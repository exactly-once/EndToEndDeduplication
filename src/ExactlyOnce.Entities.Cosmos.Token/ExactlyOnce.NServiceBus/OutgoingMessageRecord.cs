using System.Collections.Generic;
using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus
{
    public class OutgoingMessageRecord : SideEffectRecord
    {
        public string Id { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}