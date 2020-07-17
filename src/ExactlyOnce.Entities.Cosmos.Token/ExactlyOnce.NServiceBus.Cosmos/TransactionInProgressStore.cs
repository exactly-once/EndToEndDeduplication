using System;
using System.Net;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.HumanInterface;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    public class TransactionInProgressStore : ITransactionInProgressStore
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
}