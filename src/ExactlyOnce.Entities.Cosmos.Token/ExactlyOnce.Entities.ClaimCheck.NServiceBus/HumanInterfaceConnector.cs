using System;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using NServiceBus;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class HumanInterfaceConnector : IConnector
    {
        readonly IMessageSession rootMessageSession;
        readonly ExactlyOnceProcessor<object> processor;
        readonly ITransactionInProgressStore transactionInProgressStore;
        readonly IMessageStore messageStore;

        public HumanInterfaceConnector(Container applicationStoreContainer, SideEffectsHandlerCollection sideEffectsHandlers, IMessageSession rootMessageSession, ITransactionInProgressStore transactionInProgressStore, IMessageStore messageStore)
        {
            this.rootMessageSession = rootMessageSession;
            this.transactionInProgressStore = transactionInProgressStore;
            this.messageStore = messageStore;
            processor = new ExactlyOnceProcessor<object>(applicationStoreContainer, sideEffectsHandlers);
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task ExecuteTransaction(string requestId, string partitionKey, Func<IConnectorMessageSession, Task> transaction)
        {
            return processor.Process(requestId, partitionKey, null, async (ctx, transactionBatch, transactionContext) =>
            {
                var session = new HumanInterfaceConnectorMessageSession(requestId, transactionBatch, transactionContext, rootMessageSession, messageStore);
                await transaction(session).ConfigureAwait(false);
                await transactionInProgressStore.BeginTransaction(requestId, partitionKey).ConfigureAwait(false);
            });
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

    }
}