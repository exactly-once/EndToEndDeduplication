using System;
using System.Collections.Generic;
using ExactlyOnce.Core;
using Newtonsoft.Json;

namespace ExactlyOnce.NServiceBus.Cosmos
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
}