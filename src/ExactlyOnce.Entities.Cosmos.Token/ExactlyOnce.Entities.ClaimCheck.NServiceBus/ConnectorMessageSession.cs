using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using NServiceBus.Extensibility;

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

    class ConnectorMessageSession : IConnectorMessageSession
    {
        string requestId;
        string partitionKey;
        readonly HttpResponse response;
        List<Func<IMessageSession, PendingTransportOperations, Task>> messageAction = new List<Func<IMessageSession, PendingTransportOperations, Task>>();
        IMessageSession messageSession;
        IOutboxStore outboxStore;
        IConnectorDeduplicationStore deduplicationStore;
        List<OutboxRecord> outboxRecords = new List<OutboxRecord>();
        SideEffectsHandlerCollection sideEffectsHandlers;
        TransactionRecordContainer transaction;

        public ConnectorMessageSession(string requestId, string partitionKey, HttpResponse response, Container applicationStateContainer, IMessageSession messageSession, IConnectorDeduplicationStore deduplicationStore, SideEffectsHandlerCollection sideEffectsHandlers, IOutboxStore outboxStore)
        {
            this.requestId = requestId;
            this.partitionKey = partitionKey;
            this.response = response;
            this.Container = applicationStateContainer;
            this.messageSession = messageSession;
            this.deduplicationStore = deduplicationStore;
            this.sideEffectsHandlers = sideEffectsHandlers;
            this.outboxStore = outboxStore;
            TransactionBatch = applicationStateContainer.CreateTransactionalBatch(new PartitionKey(partitionKey));
        }

        public async Task<bool> Open()
        {
            transaction = new TransactionRecordContainer(Container, partitionKey);
            await transaction.Load().ConfigureAwait(false);

            if (transaction.MessageId != null)
            {
                var previousResponse = await FinishProcessingPreviousMessage().ConfigureAwait(false);
                if (previousResponse != null)
                {
                    response.UpdateFromStore(previousResponse);
                    return false;
                }
            }

            //Check the de-duplication store if we already processed that message. Has to be done after loading the transaction.
            var deduplicationResult = await deduplicationStore.HasBeenProcessed(requestId).ConfigureAwait(false);
            if (deduplicationResult != null)
            {
                response.UpdateFromStore(deduplicationResult);
                return false;
            }

            return true;
        }

        public async Task Commit()
        {
            var pendingOperations = new PendingTransportOperations();

            foreach (var messageOperation in messageAction)
            {
                await messageOperation(messageSession, pendingOperations).ConfigureAwait(false);
            }

            foreach (var pendingOperation in pendingOperations.Operations)
            {
                outboxRecords.Add(pendingOperation.ToOutboxRecord());
            }

            var responseRecord = response.ToOutboxRecord(requestId);
            outboxRecords.Add(responseRecord);

            var attemptId = Guid.NewGuid();

            await outboxStore.Store(attemptId, new OutboxState(partitionKey, requestId, outboxRecords)).ConfigureAwait(false);
            await transaction.CommitTransactionState(requestId, attemptId, TransactionBatch).ConfigureAwait(false);
            
            await PrepareSideEffects(outboxRecords, transaction).ConfigureAwait(false);
            await CommitSideEffects(outboxRecords).ConfigureAwait(false);

            await outboxStore.Remove(attemptId).ConfigureAwait(false);
            await transaction.ClearTransactionState().ConfigureAwait(false);
        }

        async Task<ResponseMessage> FinishProcessingPreviousMessage()
        {
            ResponseMessage responseToCurrentRequest = null;
            var previousOutboxState = await outboxStore.Get(transaction.AttemptId).ConfigureAwait(false);
            if (previousOutboxState != null)
            {
                var responseRecord = previousOutboxState.Records.Single(x => x.Type == "HttpResponse");
                if (transaction.MessageId == requestId)
                {
                    responseToCurrentRequest = responseRecord.ToResponseMessage();
                }

                if (!transaction.Prepared)
                {
                    await PrepareSideEffects(previousOutboxState.Records, transaction).ConfigureAwait(false);
                }
                await CommitSideEffects(previousOutboxState.Records).ConfigureAwait(false);
                await outboxStore.Remove(transaction.AttemptId).ConfigureAwait(false);
            }

            await transaction.ClearTransactionState().ConfigureAwait(false);

            //Non-null if we are attempting to process the same message again.
            return responseToCurrentRequest;
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

        public TransactionalBatch TransactionBatch { get; }
        public Container Container { get; }

        public void Send(object message, SendOptions options)
        {
            messageAction.Add((s, o) =>
            {
                options.GetExtensions().Set(o);
                return s.Send(message, options ?? new SendOptions());
            });
        }

        public void Publish(object message, PublishOptions options)
        {
            messageAction.Add((s, o) =>
            {
                options.GetExtensions().Set(o);
                return s.Publish(message, options ?? new PublishOptions());
            });
        }
    }
}