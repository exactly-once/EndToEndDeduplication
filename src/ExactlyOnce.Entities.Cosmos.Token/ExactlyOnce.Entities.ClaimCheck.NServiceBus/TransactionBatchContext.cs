using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class TransactionBatchContext : ITransactionBatchContext
    {
        readonly TransactionalBatch batch;
        readonly PartitionKey partitionKey;

        public TransactionBatchContext(TransactionalBatch batch, Container applicationStateContainer, PartitionKey partitionKey)
        {
            this.batch = batch;
            this.partitionKey = partitionKey;
            Container = applicationStateContainer;
        }

        public void CreateItem<T>(T item, TransactionalBatchItemRequestOptions options = null)
        {
            batch.CreateItem(item, options);
        }

        public void ReplaceItem<T>(string id, T item, TransactionalBatchItemRequestOptions options = null)
        {
            batch.ReplaceItem(id, item, options);
        }

        public void UpsertItem<T>(T item, TransactionalBatchItemRequestOptions options = null)
        {
            batch.UpsertItem(item, options);
        }

        public Task<ItemResponse<T>> ReadItemAsync<T>(string id, ItemRequestOptions requestOptions = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Container.ReadItemAsync<T>(id, partitionKey, requestOptions, cancellationToken);
        }

        public Container Container { get; }
    }
}