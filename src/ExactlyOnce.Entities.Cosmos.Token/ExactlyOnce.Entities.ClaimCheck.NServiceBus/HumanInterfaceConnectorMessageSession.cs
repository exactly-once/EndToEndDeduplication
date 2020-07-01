using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class HumanInterfaceConnectorMessageSession : IConnectorMessageSession
    {
        readonly ITransactionContext transactionContext;
        readonly IMessageSession rootSession;

        public HumanInterfaceConnectorMessageSession(
            ITransactionBatchContext transactionBatch, 
            ITransactionContext transactionContext,
            IMessageSession rootSession)
        {
            this.TransactionBatch = transactionBatch;
            this.transactionContext = transactionContext;
            this.rootSession = rootSession;
        }

        public Task Send(object message, SendOptions options)
        {
            options.GetExtensions().Set(transactionContext);
            return rootSession.Send(message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            options.GetExtensions().Set(transactionContext);
            return rootSession.Send(messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            options.GetExtensions().Set(transactionContext);
            return rootSession.Publish(message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            publishOptions.GetExtensions().Set(transactionContext);
            return rootSession.Publish(messageConstructor, publishOptions);
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
    }
}