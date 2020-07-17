using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.Core;
using ExactlyOnce.NServiceBus.Web.MachineInterface;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    class HumanInterfaceConnector<TPartition> : IHumanInterfaceConnector<TPartition>
    {
        readonly IApplicationStateStore<TPartition> applicationStateStore;
        readonly IMessageSession rootMessageSession;
        readonly ExactlyOnceProcessor<object> processor;
        readonly ITransactionInProgressStore transactionInProgressStore;
        readonly IMessageStore messageStore;

        public HumanInterfaceConnector(IApplicationStateStore<TPartition> applicationStateStore, 
            IEnumerable<ISideEffectsHandler> sideEffectsHandlers, 
            IMessageSession rootMessageSession, 
            IDispatchMessages dispatcher, 
            ITransactionInProgressStore transactionInProgressStore, 
            IMessageStore messageStore)
        {
            this.applicationStateStore = applicationStateStore;
            this.rootMessageSession = rootMessageSession;
            this.transactionInProgressStore = transactionInProgressStore;
            this.messageStore = messageStore;

            var allHandlers = sideEffectsHandlers.Concat(new ISideEffectsHandler[]
            {
                new WebMessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher),
                new TransactionInProgressSideEffectHandler(transactionInProgressStore),
            });

            processor = new ExactlyOnceProcessor<object>(allHandlers.ToArray(), new NServiceBusDebugLogger());
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public async Task<TResult> ExecuteTransaction<TResult>(string requestId, TPartition partitionKey, Func<IHumanInterfaceConnectorMessageSession, Task<TResult>> transaction) 
        {
            var transactionRecordContainer = applicationStateStore.Create(partitionKey);

            var outcome = await processor.Process(requestId, transactionRecordContainer, null, async (ctx, transactionContext) =>
            {
                var session = new HumanInterfaceConnectorMessageSession(requestId, transactionContext, rootMessageSession, messageStore);
                var result =  await transaction(session).ConfigureAwait(false);
                await transactionInProgressStore.BeginTransaction(requestId, transactionRecordContainer.UniqueIdentifier).ConfigureAwait(false);
                return ProcessingResult<TResult>.Successful(result);
            });

            return outcome.Value; //Duplicate check is ignored in the human interface
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

    }
}