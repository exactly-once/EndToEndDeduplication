using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class TransactionInProgressStore : ITransactionInProgressStore
    {
        readonly Container container;

        public TransactionInProgressStore(Container container)
        {
            this.container = container;
        }

        public Task BeginTransaction(string transactionId, string partitionKey)
        {
            return container.UpsertItemAsync(new TransactionInProgressRecord
            {
                Id = transactionId,
                EntityPartitionKey = partitionKey
            }, new PartitionKey(transactionId));
        }

        public async Task CompleteTransaction(string transactionId)
        {
            var response = await container.DeleteItemStreamAsync(transactionId, new PartitionKey(transactionId));
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new Exception("Unexpected error while clearing transaction-in-progress state");
            }
        }
    }

    public class TransactionInProgressRecord
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string EntityPartitionKey { get; set; }
        [JsonProperty("_etag")]
        public string Etag;
    }
}