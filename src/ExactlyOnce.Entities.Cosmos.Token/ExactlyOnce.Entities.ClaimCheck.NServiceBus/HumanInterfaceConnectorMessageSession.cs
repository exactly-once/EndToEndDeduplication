using System;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using NServiceBus;
using NServiceBus.Extensibility;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class HumanInterfaceConnectorMessageSession : IConnectorMessageSession
    {
        readonly string requestId;
        readonly ITransactionContext transactionContext;
        readonly IMessageSession rootSession;
        readonly IMessageStore messageStore;

        public HumanInterfaceConnectorMessageSession(
            string requestId,
            ITransactionBatchContext transactionBatch, 
            ITransactionContext transactionContext,
            IMessageSession rootSession, 
            IMessageStore messageStore)
        {
            this.TransactionBatch = transactionBatch;
            this.requestId = requestId;
            this.transactionContext = transactionContext;
            this.rootSession = rootSession;
            this.messageStore = messageStore;
        }

        public Task Send(object message, SendOptions options)
        {
            return CaptureMessages(s => s.Send(message, options), options);
        }
        
        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return CaptureMessages(s => s.Send(messageConstructor, options), options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return CaptureMessages(s => s.Publish(message, options), options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions options)
        {
            return CaptureMessages(s => s.Publish(messageConstructor, options), options);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            throw new NotSupportedException("Subscribing from connector is not supported.");
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            throw new NotSupportedException("Unsubscribing from connector is not supported.");
        }

        public ITransactionBatchContext TransactionBatch { get; }

        async Task CaptureMessages(Func<IMessageSession, Task> operation, ExtendableOptions options)
        {
            var pendingOperations = new PendingTransportOperations();
            options.GetExtensions().Set(pendingOperations);

            await operation(rootSession).ConfigureAwait(false);

            var messageRecords = pendingOperations.Operations.Select(o => o.ToMessageRecord(requestId, transactionContext.AttemptId)).Cast<SideEffectRecord>().ToList();
            var messagesToCheck = pendingOperations.Operations.Select(o => o.ToCheck(transactionContext.AttemptId)).ToArray();

            await transactionContext.AddSideEffects(messageRecords).ConfigureAwait(false);
            await messageStore.Create(messagesToCheck).ConfigureAwait(false);
        }
    }
}