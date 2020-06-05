using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface ITransactionBatchContext
    {
        void CreateItem<T>(T item, TransactionalBatchItemRequestOptions options = null);
        void ReplaceItem<T>(string id, T item, TransactionalBatchItemRequestOptions options = null);
        void UpsertItem<T>(T item, TransactionalBatchItemRequestOptions options = null);

        Task<ItemResponse<T>> ReadItemAsync<T>(
            string id,
            ItemRequestOptions requestOptions = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Application state container. Only for querying data.
        /// </summary>
        Container Container { get; }
    }
}