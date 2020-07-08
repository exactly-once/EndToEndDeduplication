using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class HumanInterfaceConnector : IHumanInterfaceConnector
    {
        readonly IMessageSession rootMessageSession;
        readonly ExactlyOnceProcessor<object, object> processor;
        readonly ITransactionInProgressStore transactionInProgressStore;
        readonly IMessageStore messageStore;

        public HumanInterfaceConnector(Container applicationStoreContainer, 
            IEnumerable<ISideEffectsHandler> sideEffectsHandlers, 
            IMessageSession rootMessageSession, 
            IDispatchMessages dispatcher, 
            ITransactionInProgressStore transactionInProgressStore, 
            IMessageStore messageStore)
        {
            this.rootMessageSession = rootMessageSession;
            this.transactionInProgressStore = transactionInProgressStore;
            this.messageStore = messageStore;

            var allHandlers = sideEffectsHandlers.Concat(new ISideEffectsHandler[]
            {
                new WebMessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher),
                new TransactionInProgressSideEffectHandler(transactionInProgressStore),
            });

            processor = new ExactlyOnceProcessor<object, object>(applicationStoreContainer, 
                new SideEffectsHandlerCollection(allHandlers.ToArray()));
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task ExecuteTransaction(string requestId, string partitionKey, Func<IHumanInterfaceConnectorMessageSession, Task> transaction)
        {
            return processor.Process(requestId, partitionKey, null, async (ctx, transactionBatch, transactionContext) =>
            {
                var session = new HumanInterfaceConnectorMessageSession(requestId, transactionBatch, transactionContext, rootMessageSession, messageStore);
                await transaction(session).ConfigureAwait(false);
                await transactionInProgressStore.BeginTransaction(requestId, partitionKey).ConfigureAwait(false);
                return ProcessingResult<object>.Successful(null);
            });
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

    }
}