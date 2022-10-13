using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.Core;
using NServiceBus;
using NServiceBus.Extensibility;

namespace ExactlyOnce.NServiceBus.Web.MachineInterface
{
    class MachineInterfaceConnectorMessageSession<TPayload> : IMachineInterfaceConnectorMessageSession<TPayload>
    {
        readonly string requestId;
        readonly IMessageSession rootSession;
        readonly IMessageStore messageStore;

        public MachineInterfaceConnectorMessageSession(
            string requestId,
            TPayload payload,
            ITransactionContext transactionContext,
            IMessageSession rootSession, 
            IMessageStore messageStore)
        {
            Payload = payload;
            TransactionContext = transactionContext;

            this.requestId = requestId;
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

        public ITransactionContext TransactionContext { get; }
        public TPayload Payload { get; }

        async Task CaptureMessages(Func<IMessageSession, Task> operation, ExtendableOptions options)
        {
            var pendingOperations = new PendingTransportOperations();
            options.GetExtensions().Set(pendingOperations);

            await operation(rootSession).ConfigureAwait(false);

            var messageRecords = pendingOperations.Operations.Select(o => o.ToMessageRecord(requestId, TransactionContext.AttemptId)).Cast<SideEffectRecord>().ToList();
            var messagesToCheck = pendingOperations.Operations.Select(o => o.ToCheck()).ToArray();

            await TransactionContext.AddSideEffects(messageRecords).ConfigureAwait(false);
            await messageStore.Create(requestId, messagesToCheck).ConfigureAwait(false);
        }
    }

    class MachineInterfaceConnectorMessageSession : IMachineInterfaceConnectorMessageSession
    {
        readonly string requestId;
        readonly IMessageSession rootSession;
        readonly IMessageStore messageStore;

        public MachineInterfaceConnectorMessageSession(
            string requestId,
            Stream payload,
            ITransactionContext transactionContext,
            IMessageSession rootSession,
            IMessageStore messageStore)
        {
            Payload = payload;
            TransactionContext = transactionContext;

            this.requestId = requestId;
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

        public ITransactionContext TransactionContext { get; }
        public Stream Payload { get; }

        async Task CaptureMessages(Func<IMessageSession, Task> operation, ExtendableOptions options)
        {
            var pendingOperations = new PendingTransportOperations();
            options.GetExtensions().Set(pendingOperations);

            await operation(rootSession).ConfigureAwait(false);

            var messageRecords = pendingOperations.Operations.Select(o => o.ToMessageRecord(requestId, TransactionContext.AttemptId)).Cast<SideEffectRecord>().ToList();
            var messagesToCheck = pendingOperations.Operations.Select(o => o.ToCheck()).ToArray();

            await TransactionContext.AddSideEffects(messageRecords).ConfigureAwait(false);
            await messageStore.Create(requestId, messagesToCheck).ConfigureAwait(false);
        }
    }
}