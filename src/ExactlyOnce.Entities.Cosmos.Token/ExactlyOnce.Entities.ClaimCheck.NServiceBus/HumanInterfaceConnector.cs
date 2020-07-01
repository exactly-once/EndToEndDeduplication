using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using NServiceBus;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class HumanInterfaceConnector : IHumanInterfaceConnector
    {
        IMessageSession rootMessageSession;
        ExactlyOnceProcessor<object> processor;
        ITransactionInProgressStore transactionInProgressStore;

        public HumanInterfaceConnector(Container applicationStoreContainer, SideEffectsHandlerCollection sideEffectsHandlers, IMessageSession rootMessageSession, ITransactionInProgressStore transactionInProgressStore)
        {
            this.rootMessageSession = rootMessageSession;
            this.transactionInProgressStore = transactionInProgressStore;
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
                var session = new HumanInterfaceConnectorMessageSession(transactionBatch, transactionContext, rootMessageSession);
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