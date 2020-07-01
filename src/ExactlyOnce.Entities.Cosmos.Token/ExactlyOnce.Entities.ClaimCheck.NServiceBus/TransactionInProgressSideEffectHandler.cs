using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class TransactionInProgressSideEffectHandler : ISideEffectsHandler
    {
        readonly ITransactionInProgressStore transactionInProgressStore;

        public TransactionInProgressSideEffectHandler(ITransactionInProgressStore transactionInProgressStore)
        {
            this.transactionInProgressStore = transactionInProgressStore;
        }

        public Task Publish(string messageId, Guid attemptId, IEnumerable<SideEffectRecord> committedSideEffects, IEnumerable<SideEffectRecord> abortedSideEffects)
        {
            return transactionInProgressStore.CompleteTransaction(messageId);
        }
    }
}