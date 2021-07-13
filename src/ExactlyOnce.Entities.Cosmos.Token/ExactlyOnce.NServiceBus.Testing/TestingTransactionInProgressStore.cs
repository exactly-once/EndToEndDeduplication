using System;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.HumanInterface;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingTransactionInProgressStore : ITransactionInProgressStore
    {
        ITransactionInProgressStore impl;
        public Task BeginTransaction(string transactionId, string partitionKey)
        {
            return impl.BeginTransaction(transactionId, partitionKey);
        }

        public Task CompleteTransaction(string transactionId)
        {
            return impl.CompleteTransaction(transactionId);
        }
    }
}