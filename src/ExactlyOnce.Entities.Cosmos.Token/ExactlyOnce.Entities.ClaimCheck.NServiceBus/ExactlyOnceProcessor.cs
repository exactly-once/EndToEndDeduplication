using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class ExactlyOnceProcessor<TContext>
    {
        readonly Container applicationStateContainer;
        readonly SideEffectsHandlerCollection sideEffectsHandlerCollection;

        public ExactlyOnceProcessor(Container applicationStateContainer, 
            SideEffectsHandlerCollection sideEffectsHandlerCollection)
        {
            this.applicationStateContainer = applicationStateContainer;
            this.sideEffectsHandlerCollection = sideEffectsHandlerCollection;
        }

        /// <summary>
        /// The algorithm stores metadata about messages that are going to be sent before the claim check entries are stored
        /// in order to prevent accumulation of garbage messages in the claim check. First, the IDs of messages that are about
        /// to be sent are stored in the transaction record with assigned attempt ID and incoming message ID. Then, the message
        /// bodies are actually uploaded to the claim check. Next, the transaction record is committed.
        ///
        /// At any point in time the OutgoingMessages collection of the transaction record may contain messages produced by
        /// processing different incoming messages and different attempts but they don't interfere with each other e.g.
        /// - Message A processing attempt 1 is started, resulting in message records stored as A-1-1 and A-1-2. Then it fails.
        /// - Message B processing attempt 1 is started, resulting in B-1-1 and B-1-2
        /// - Message B processing attempt 1 is finished. No claim check message are removed.
        /// - Message A processing attempt 2 is started, resulting in message records stored as A-2-1.
        /// - Message A processing attempt 2 is finished. Messages A-1-1 and A-1-2 are deleted.
        /// </summary>
        public async Task Process(string currentMessageId, string partitionKey, TContext context,
            Func<TContext, ITransactionBatchContext, ITransactionContext, Task> invokeMessageHandlers)
        {
            var transaction = new TransactionRecordContainer(applicationStateContainer, partitionKey);
            await transaction.Load().ConfigureAwait(false);

            var previousTransactionId = transaction.MessageId;
            if (previousTransactionId != null)
            {
                await FinishProcessing(transaction).ConfigureAwait(false);

                if (previousTransactionId == currentMessageId)
                {
                    //Duplicate
                    return;
                }
            }

            var attemptId = Guid.NewGuid();
            var batch = applicationStateContainer.CreateTransactionalBatch(new PartitionKey(partitionKey));
            var batchContext = new TransactionBatchContext(batch, applicationStateContainer, new PartitionKey(partitionKey));

            await invokeMessageHandlers(context, batchContext, new TransactionContext(attemptId, transaction)).ConfigureAwait(false);

            await transaction.CommitTransactionState(currentMessageId, attemptId, batch).ConfigureAwait(false);
            await FinishProcessing(transaction).ConfigureAwait(false);
        }

        async Task FinishProcessing(TransactionRecordContainer transaction)
        {
            await sideEffectsHandlerCollection.Publish(transaction.MessageId, transaction.AttemptId, 
                transaction.CommittedSideEffects, 
                transaction.AbortedSideEffects).ConfigureAwait(false);

            await transaction.ClearTransactionState().ConfigureAwait(false);
        }

        class TransactionContext : ITransactionContext
        {
            readonly TransactionRecordContainer transactionRecordContainer;

            public TransactionContext(Guid attemptId, TransactionRecordContainer transactionRecordContainer)
            {
                AttemptId = attemptId;
                this.transactionRecordContainer = transactionRecordContainer;
            }

            public Guid AttemptId { get; }
            public Task AddSideEffects(List<SideEffectRecord> messageRecords)
            {
                return transactionRecordContainer.AddSideEffects(messageRecords);
            }
        }
    }
}