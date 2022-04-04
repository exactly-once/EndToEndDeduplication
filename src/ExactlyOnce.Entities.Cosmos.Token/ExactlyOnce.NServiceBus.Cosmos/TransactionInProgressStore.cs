using System;
using System.Net;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.HumanInterface;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    using System.Collections.Generic;

    public class TransactionInProgressStore : ITransactionInProgressStore<string>
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
                EntityPartitionKey = partitionKey,
                StartedAt = DateTimeOffset.UtcNow
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

        public async Task<IEnumerable<TransactionInProgress<string>>> GetUnfinishedTransactions(int limit)
        {
            var results = new List<TransactionInProgress<string>>();
            var cutOffTime = DateTimeOffset.UtcNow.AddSeconds(-30);
            var queryDefinition = new QueryDefinition("select * from tx where tx.StartedAt < @cutOffTime")
                .WithParameter("@cutOffTime", cutOffTime);

            var queryRequestOptions = new QueryRequestOptions
            {
                MaxItemCount = limit
            };
            using (var feedIterator = container.GetItemQueryIterator<TransactionInProgressRecord>(queryDefinition, null, queryRequestOptions))
            {
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync();
                    foreach (var record in response)
                    {
                        var tip = new TransactionInProgress<string>(record.Id, record.EntityPartitionKey);
                        results.Add(tip);
                    }
                }
            }

            return results;
        }
    }
}