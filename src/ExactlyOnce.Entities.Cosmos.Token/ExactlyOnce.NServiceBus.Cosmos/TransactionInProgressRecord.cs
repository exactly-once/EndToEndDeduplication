using Newtonsoft.Json;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    public class TransactionInProgressRecord
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string EntityPartitionKey { get; set; }
        [JsonProperty("_etag")]
        public string Etag;
    }
}