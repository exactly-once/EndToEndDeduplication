using System;
using Newtonsoft.Json;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class TransactionRecord
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string MessageId { get; set; }
        public Guid? AttemptId { get; set; }
        public bool MessagesChecked { get; set; }
        public string PartitionId { get; set; }

        [JsonProperty("_etag")]
        public string Etag;
    }
}