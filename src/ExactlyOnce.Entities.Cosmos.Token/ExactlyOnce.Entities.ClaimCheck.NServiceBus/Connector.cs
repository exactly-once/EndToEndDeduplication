using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using NServiceBus;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class Connector : IConnector
    {
        Container applicationStoreContainer;
        IMessageSession messageSession;
        IConnectorDeduplicationStore deduplicationStore;
        SideEffectsHandlerCollection sideEffectsHandlers;
        IOutboxStore outboxStore;

        public Connector(Container applicationStoreContainer, IOutboxStore outboxStore, SideEffectsHandlerCollection sideEffectsHandlers, IConnectorDeduplicationStore deduplicationStore, IMessageSession messageSession)
        {
            this.applicationStoreContainer = applicationStoreContainer;
            this.outboxStore = outboxStore;
            this.sideEffectsHandlers = sideEffectsHandlers;
            this.deduplicationStore = deduplicationStore;
            this.messageSession = messageSession;
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public async Task ExecuteTransaction(string requestId, string partitionKey, HttpResponse currentResponse,
            Func<IConnectorMessageSession, Task<int>> transaction)
        {
            var session = new ConnectorMessageSession(requestId, partitionKey, currentResponse, applicationStoreContainer, messageSession, deduplicationStore, sideEffectsHandlers, outboxStore);
            if (await session.Open().ConfigureAwait(false))
            {
                await transaction(session).ConfigureAwait(false);
                await session.Commit().ConfigureAwait(false);
            }
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }
    }
}