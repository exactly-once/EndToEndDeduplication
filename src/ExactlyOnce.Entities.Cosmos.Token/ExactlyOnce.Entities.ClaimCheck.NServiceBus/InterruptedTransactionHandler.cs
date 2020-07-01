using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class InterruptedTransactionHandler
    {
        Container applicationStateContainer;
        SideEffectsHandlerCollection sideEffectsHandlers;
        IOutboxStore outboxStore;

        public async Task FinishTransaction(OutboxState outboxState)
        {
            var transaction = new TransactionRecordContainer(applicationStateContainer, outboxState.PartitionId);
            if (!transaction.Prepared)
            {
                await PrepareSideEffects(outboxState.Records, transaction).ConfigureAwait(false);
            }
            await CommitSideEffects(outboxState.Records).ConfigureAwait(false);
            await outboxStore.Remove(transaction.AttemptId).ConfigureAwait(false);

            await transaction.ClearTransactionState().ConfigureAwait(false);
        }

        async Task CommitSideEffects(IEnumerable<OutboxRecord> previousOutboxState)
        {
            await sideEffectsHandlers.Commit(previousOutboxState).ConfigureAwait(false);
        }

        async Task PrepareSideEffects(IEnumerable<OutboxRecord> outboxState, TransactionRecordContainer transactionRecordContainer)
        {
            //Stores messages in the message store
            await sideEffectsHandlers.Prepare(outboxState).ConfigureAwait(false);
            await transactionRecordContainer.MarkMessagesChecked().ConfigureAwait(false);
        }
    }
}