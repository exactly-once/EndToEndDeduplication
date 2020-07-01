using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class TransactionRecord
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string MessageId { get; set; }
        public Guid? AttemptId { get; set; }
        
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public List<SideEffectRecord> SideEffects { get; set; } = new List<SideEffectRecord>();
        public string PartitionId { get; set; }
        [JsonProperty("_etag")]
        public string Etag;
    }

    public class OutgoingMessageRecord : SideEffectRecord
    {
        public string Id { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public abstract class SideEffectRecord
    {
        public string IncomingId { get; set; }
        public Guid AttemptId { get; set; }
    }
}