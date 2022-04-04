using Newtonsoft.Json;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    using System;

    public class TransactionInProgressRecord
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string EntityPartitionKey { get; set; }
        [JsonProperty("_etag")]
        public string Etag;
        public DateTimeOffset StartedAt { get; set; }
    }
}