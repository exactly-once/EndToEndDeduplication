using System;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.HumanInterface;

namespace ExactlyOnce.NServiceBus.Testing
{
    using System.Collections.Generic;

    public class TestingTransactionInProgressStore<T> : ITransactionInProgressStore<T>
    {
        ITransactionInProgressStore<T> impl;
        public Task BeginTransaction(string transactionId, T partitionKey)
        {
            return impl.BeginTransaction(transactionId, partitionKey);
        }

        public Task CompleteTransaction(string transactionId)
        {
            return impl.CompleteTransaction(transactionId);
        }

        public Task<IEnumerable<TransactionInProgress<T>>> GetUnfinishedTransactions(int limit)
        {
            return impl.GetUnfinishedTransactions(limit);
        }
    }
}