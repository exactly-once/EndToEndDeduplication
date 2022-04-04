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
    using System.Threading;

    class HumanInterfaceConnector<TPartition> : IHumanInterfaceConnector<TPartition>
    {
        const int TransactionInProgressQueryLimit = 10;
        static readonly TimeSpan TransactionInProgressQueryInterval = TimeSpan.FromSeconds(5);

        readonly IApplicationStateStore<TPartition> applicationStateStore;
        readonly IMessageSession rootMessageSession;
        readonly ExactlyOnceProcessor<object> processor;
        readonly ITransactionInProgressStore<TPartition> transactionInProgressStore;
        readonly IMessageStore messageStore;
        readonly int transactionInProgressQueryLimit;
        readonly TimeSpan transactionInProgressQueryInterval;
        CancellationTokenSource tokenSource;
        Task completeTask;

        public HumanInterfaceConnector(IApplicationStateStore<TPartition> applicationStateStore, 
            IEnumerable<ISideEffectsHandler> sideEffectsHandlers, 
            IMessageSession rootMessageSession, 
            IDispatchMessages dispatcher, 
            ITransactionInProgressStore<TPartition> transactionInProgressStore, 
            IMessageStore messageStore,
            int? transactionInProgressQueryLimit = null,
            TimeSpan? transactionInProgressQueryInterval = null)
        {
            this.applicationStateStore = applicationStateStore;
            this.rootMessageSession = rootMessageSession;
            this.transactionInProgressStore = transactionInProgressStore;
            this.messageStore = messageStore;
            this.transactionInProgressQueryLimit = transactionInProgressQueryLimit ?? TransactionInProgressQueryLimit;
            this.transactionInProgressQueryInterval = transactionInProgressQueryInterval ?? TransactionInProgressQueryInterval;

            var allHandlers = sideEffectsHandlers.Concat(new ISideEffectsHandler[]
            {
                new WebMessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher),
                new TransactionInProgressSideEffectHandler<TPartition>(transactionInProgressStore),
            });

            processor = new ExactlyOnceProcessor<object>(allHandlers.ToArray(), new NServiceBusDebugLogger());
        }

        public Task Start()
        {
            tokenSource = new CancellationTokenSource();
            completeTask = Task.Run(() => CompleteTransactions(tokenSource.Token));

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

        async Task CompleteTransactions(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var transactionsFound = false;
                var unfinishedTransactions = await transactionInProgressStore.GetUnfinishedTransactions(transactionInProgressQueryLimit);
                foreach (var transaction in unfinishedTransactions)
                {
                    transactionsFound = true;
                    var transactionRecordContainer = applicationStateStore.Create(transaction.PartitionKey);
                    await processor.FinishProcessing(transaction.TransactionId, transactionRecordContainer).ConfigureAwait(false);
                }

                if (!transactionsFound)
                {
                    try
                    {
                        await Task.Delay(transactionInProgressQueryInterval, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        //Ignore
                    }
                }
            }
        }

        public async Task Stop()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();

                if (completeTask != null)
                {
                    await completeTask.ConfigureAwait(false);
                }
            }
        }

    }
}