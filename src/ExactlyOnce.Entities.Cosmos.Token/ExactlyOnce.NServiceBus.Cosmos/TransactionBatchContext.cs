using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    class TransactionBatchContext : ITransactionBatchContext
    {
        readonly PartitionKey partitionKey;
        public TransactionalBatch Batch { get; }

        public TransactionBatchContext(TransactionalBatch batch, Container applicationStateContainer, PartitionKey partitionKey)
        {
            this.Batch = batch;
            this.partitionKey = partitionKey;
            Container = applicationStateContainer;
        }

        public void CreateItem<T>(T item, TransactionalBatchItemRequestOptions options = null)
        {
            Batch.CreateItem(item, options);
        }

        public void ReplaceItem<T>(string id, T item, TransactionalBatchItemRequestOptions options = null)
        {
            Batch.ReplaceItem(id, item, options);
        }

        public void UpsertItem<T>(T item, TransactionalBatchItemRequestOptions options = null)
        {
            Batch.UpsertItem(item, options);
        }

        public Task<ItemResponse<T>> ReadItemAsync<T>(string id, ItemRequestOptions requestOptions = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Container.ReadItemAsync<T>(id, partitionKey, requestOptions, cancellationToken);
        }

        public Container Container { get; }
    }
}