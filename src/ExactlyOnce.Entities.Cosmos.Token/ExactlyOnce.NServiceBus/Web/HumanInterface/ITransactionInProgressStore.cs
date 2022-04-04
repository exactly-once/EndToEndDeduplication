using System.Threading.Tasks;

namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    using System.Collections.Generic;

    public interface ITransactionInProgressStore<TPartition>
    {
        Task BeginTransaction(string transactionId, TPartition partitionKey);
        Task CompleteTransaction(string transactionId);
        Task<IEnumerable<TransactionInProgress<TPartition>>> GetUnfinishedTransactions(int limit);
    }
}