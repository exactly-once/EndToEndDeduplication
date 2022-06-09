using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Core
{
    public class ExactlyOnceProcessor<TContext>
    {
        readonly ISideEffectsHandler[] sideEffectsHandlers;
        readonly IDebugLogger log;

        public ExactlyOnceProcessor(ISideEffectsHandler[] sideEffectsHandlers, IDebugLogger log)
        {
            this.sideEffectsHandlers = sideEffectsHandlers;
            this.log = log;
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
        public async Task<ProcessingResult<TResult>> Process<TResult>(string currentMessageId, ITransactionRecordContainer transaction, TContext context,
            Func<TContext, ITransactionContext, Task<ProcessingResult<TResult>>> invokeMessageHandlers)
        {
            await transaction.Load().ConfigureAwait(false);

            var previousTransactionId = transaction.MessageId;
            if (previousTransactionId != null)
            {
                log.Log($"Unfinished transaction {previousTransactionId} detected. Attempting to complete that transaction.");
                await FinishProcessing(transaction).ConfigureAwait(false);

                if (previousTransactionId == currentMessageId)
                {
                    log.Log($"Duplicate message {currentMessageId} detected. Ignoring.");
                    return ProcessingResult<TResult>.Duplicate;
                }
            }

            var attemptId = Guid.NewGuid();
            log.Log($"Beginning attempt {attemptId} to process message {currentMessageId}.");

            await transaction.BeginStateTransition().ConfigureAwait(false);

            var result = await invokeMessageHandlers(context, new TransactionContext(attemptId, transaction)).ConfigureAwait(false);
            if (result.IsDuplicate)
            {
                log.Log($"Duplicate message {currentMessageId} detected. Ignoring.");
                return result;
            }
            log.Log($"Committing transaction for attempt {attemptId} message {currentMessageId}.");

            await transaction.CommitStateTransition(currentMessageId, attemptId).ConfigureAwait(false);
            await FinishProcessing(transaction).ConfigureAwait(false);

            return result;
        }

        public async Task<ProcessingResult<TResult>> ProcessWithoutApplyingSideEffects<TResult>(string currentMessageId, ITransactionRecordContainer transaction, TContext context,
            Func<TContext, ITransactionContext, Task<ProcessingResult<TResult>>> invokeMessageHandlers)
        {
            var previousTransactionId = transaction.MessageId;
            if (previousTransactionId != null)
            {
                //We have to throw here because otherwise we would have to apply the side effects of the previous transaction.
                throw new Exception("Another transaction in progress: " + previousTransactionId);
            }

            var attemptId = Guid.NewGuid();
            log.Log($"Beginning attempt {attemptId} to process message {currentMessageId}.");

            await transaction.BeginStateTransition().ConfigureAwait(false);

            var result = await invokeMessageHandlers(context, new TransactionContext(attemptId, transaction)).ConfigureAwait(false);
            if (result.IsDuplicate)
            {
                log.Log($"Duplicate message {currentMessageId} detected. Ignoring.");
                return result;
            }
            log.Log($"Committing transaction for attempt {attemptId} message {currentMessageId}.");

            await transaction.CommitStateTransition(currentMessageId, attemptId).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> TryApplySideEffects(string currentMessageId, ITransactionRecordContainer transaction)
        {
            await transaction.Load().ConfigureAwait(false);

            var previousTransactionId = transaction.MessageId;
            if (previousTransactionId == null || previousTransactionId != currentMessageId)
            {
                return false;
            }
            await FinishProcessing(transaction).ConfigureAwait(false);
            return true;
        }

        async Task FinishProcessing(ITransactionRecordContainer transaction)
        {
            log.Log($"Publishing side effects of processing message {transaction.MessageId} (successful attempt {transaction.AttemptId}).");
            await PublishSideEffects(transaction).ConfigureAwait(false);
            log.Log($"Clearing transaction state for message {transaction.MessageId}.");
            await transaction.ClearTransactionState().ConfigureAwait(false);
        }

        async Task PublishSideEffects(ITransactionRecordContainer transaction)
        {
            foreach (var handler in sideEffectsHandlers)
            {
                await handler.Publish(transaction.MessageId,
                    transaction.AttemptId,
                    transaction.CommittedSideEffects,
                    transaction.AbortedSideEffects).ConfigureAwait(false);
            }
        }

        class TransactionContext : ITransactionContext
        {
            public TransactionContext(Guid attemptId, ITransactionRecordContainer transactionRecordContainer)
            {
                AttemptId = attemptId;
                TransactionRecordContainer = transactionRecordContainer;
            }

            public Guid AttemptId { get; }
            public ITransactionRecordContainer TransactionRecordContainer { get; }

            public Task AddSideEffect(SideEffectRecord sideEffectRecord)
            {
                return TransactionRecordContainer.AddSideEffect(sideEffectRecord);
            }

            public Task AddSideEffects(List<SideEffectRecord> sideEffectRecords)
            {
                return TransactionRecordContainer.AddSideEffects(sideEffectRecords);
            }
        }
    }
}