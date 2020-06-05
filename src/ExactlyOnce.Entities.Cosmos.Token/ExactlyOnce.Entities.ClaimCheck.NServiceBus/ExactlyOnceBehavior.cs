using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class ExactlyOnceBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        readonly CorrelationManager correlation;
        readonly Container applicationStateContainer;
        readonly SideEffectsHandlerCollection sideEffectsHandlers;
        readonly IOutboxStore outboxStore;
        readonly IMessageStore messageStore;

        public ExactlyOnceBehavior(Container applicationStateContainer, IOutboxStore outboxStore, IMessageStore messageStore, SideEffectsHandlerCollection sideEffectsHandlers, CorrelationManager correlation)
        {
            this.applicationStateContainer = applicationStateContainer;
            this.outboxStore = outboxStore;
            this.messageStore = messageStore;
            this.sideEffectsHandlers = sideEffectsHandlers;
            this.correlation = correlation;
        }

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            if (!correlation.TryGetPartitionKey(context.Message.MessageType, context.Headers, context.Message.Instance,
                out var partitionKey))
            {
                //This message has not been mapped but correlation manager allows it to be processed.
                await next().ConfigureAwait(false);
                return;
            }

            var transaction = new TransactionRecordContainer(applicationStateContainer, partitionKey);
            await transaction.Load().ConfigureAwait(false);

            var previousTransactionId = transaction.MessageId;
            if (previousTransactionId != null)
            {
                await FinishProcessingPreviousMessage(transaction, context).ConfigureAwait(false);

                if (previousTransactionId == context.MessageId)
                {
                    //Duplicate
                    return;
                }
            }

            //Check the de-duplication store if we already processed that message. Has to be done after loading the transaction.
            var messageExists = await messageStore.CheckExists(context.MessageId).ConfigureAwait(false);
            if (!messageExists)
            {
                //Duplicate
                return;
            }

            var outboxRecordList = new List<OutboxRecord>();
            context.Extensions.Set(outboxRecordList);
            var pendingOperations = new PendingTransportOperations();
            context.Extensions.Set(pendingOperations);

            var batch = applicationStateContainer.CreateTransactionalBatch(new PartitionKey(partitionKey));
            var batchContext = new TransactionBatchContext(batch, applicationStateContainer, new PartitionKey(partitionKey));
            context.Extensions.Set<ITransactionBatchContext>(batchContext);

            await next().ConfigureAwait(false);

            //After all side effects are recorded, we append messages to ensure messages are pushed last.
            foreach (var transportOperation in pendingOperations.Operations)
            {
                outboxRecordList.Add(transportOperation.ToOutboxRecord());
            }

            var attemptId = Guid.NewGuid();
            await outboxStore.Store(attemptId, new OutboxState(partitionKey, context.MessageId, outboxRecordList));

            await transaction.CommitTransactionState(context.MessageId, attemptId, batch).ConfigureAwait(false);

            await PrepareSideEffects(outboxRecordList, transaction).ConfigureAwait(false);

            await CommitSideEffects(outboxRecordList).ConfigureAwait(false);

            //Need to ensure token is removed before TransactionId is modified and persisted
            await outboxStore.Remove(attemptId).ConfigureAwait(false);
            await messageStore.Delete(context.MessageId).ConfigureAwait(false);

            await transaction.ClearTransactionState().ConfigureAwait(false);
        }

        async Task FinishProcessingPreviousMessage(TransactionRecordContainer transaction, IMessageProcessingContext context)
        {
            var messageId = context.MessageId;

            var previousOutboxState = await outboxStore.Get(transaction.AttemptId).ConfigureAwait(false);
            if (previousOutboxState != null)
            {
                if (!transaction.Prepared)
                {
                    await PrepareSideEffects(previousOutboxState.Records, transaction).ConfigureAwait(false);
                }

                await CommitSideEffects(previousOutboxState.Records).ConfigureAwait(false);
                await outboxStore.Remove(transaction.AttemptId).ConfigureAwait(false);
            }

            await messageStore.Delete(messageId).ConfigureAwait(false);

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