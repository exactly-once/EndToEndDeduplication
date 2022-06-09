using System;
using System.Threading.Tasks;


namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    using Core;
    using MachineInterface;
    using NServiceBus;
    using global::NServiceBus;
    using global::NServiceBus.Transport;

    class HumanInterfaceConnector<TPartition> : IHumanInterfaceConnector<TPartition>
    {
        readonly IApplicationStateStore<TPartition> applicationStateStore;
        readonly IMessageSession rootMessageSession;
        readonly IDispatchMessages dispatcher;
        readonly string localAddress;
        readonly ExactlyOnceProcessor<object> processor;
        readonly IMessageStore messageStore;
        readonly TimeSpan delay;

        public HumanInterfaceConnector(IApplicationStateStore<TPartition> applicationStateStore,
            IMessageSession rootMessageSession,
            IDispatchMessages dispatcher,
            string localAddress,
            IMessageStore messageStore,
            TimeSpan delay)
        {
            this.applicationStateStore = applicationStateStore;
            this.rootMessageSession = rootMessageSession;
            this.dispatcher = dispatcher;
            this.localAddress = localAddress;
            this.messageStore = messageStore;
            this.delay = delay;

            processor = new ExactlyOnceProcessor<object>(Array.Empty<ISideEffectsHandler>(), new NServiceBusDebugLogger());
        }


        public async Task<T> ExecuteTransaction<T>(TPartition partitionKey, Func<IHumanInterfaceConnectorMessageSession, Task<T>> transaction)
        {
            var transactionRecordContainer = applicationStateStore.Create(partitionKey);
            var attemptId = Guid.NewGuid();
            var transactionId = attemptId.ToString();

            //Attempt id and Incoming ID are the same as there is always going to be a single attempt per transaction.
            await transactionRecordContainer.Load().ConfigureAwait(false);
            await transactionRecordContainer.AddSideEffect(new CompleteTransactionMessageRecord
            {
                AttemptId = attemptId,
                IncomingId = transactionId
            }).ConfigureAwait(false);

            await messageStore.Create(transactionId, new[]
            {
                new Message(transactionId, Array.Empty<byte>())
            }).ConfigureAwait(false);

            //When Process succeeds it is guaranteed that the complete message is in the queue and the token for processing it is not deleted
            //The token can only be deleted if the Behavior successfully executed process
            var r = await processor.ProcessWithoutApplyingSideEffects(transactionId, transactionRecordContainer, null, async (ctx, transactionContext) =>
            {
                if (!await messageStore.CheckExists(transactionId))
                {
                    throw new Exception("Transaction timed out.");
                }

                var session = new HumanInterfaceConnectorMessageSession(transactionId, transactionContext, rootMessageSession, messageStore);

                var result = await transaction(session).ConfigureAwait(false);

                await HumanInterfaceCompleteMessageHandler.Enqueue(transactionId, partitionKey, 1, delay, localAddress, dispatcher).ConfigureAwait(false);

                return ProcessingResult<T>.Successful(result);
            });
            return r.Value;
        }
    }
}