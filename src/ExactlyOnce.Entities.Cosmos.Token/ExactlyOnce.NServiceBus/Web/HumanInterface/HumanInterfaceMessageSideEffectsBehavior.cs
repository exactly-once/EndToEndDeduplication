namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using global::NServiceBus.Logging;
    using global::NServiceBus.Pipeline;
    using global::NServiceBus.Transport;
    using MachineInterface;

    class HumanInterfaceMessageSideEffectsBehavior<TPartition> : Behavior<IIncomingPhysicalMessageContext>
    {
        static readonly ILog log = LogManager.GetLogger<HumanInterfaceMessageSideEffectsBehavior<TPartition>>();

        readonly int maxAttempts;
        readonly TimeSpan delayBetweenAttempts;
        readonly IApplicationStateStore<TPartition> applicationStateStore;
        readonly IDispatchMessages dispatcher;
        readonly IMessageStore messageStore;
        readonly ExactlyOnceProcessor<object> processor;
        readonly string localAddress;

        public HumanInterfaceMessageSideEffectsBehavior(IApplicationStateStore<TPartition> applicationStateStore,
            IEnumerable<ISideEffectsHandler> sideEffectsHandlers,
            IDispatchMessages dispatcher,
            IMessageStore messageStore, string localAddress, int maxAttempts, TimeSpan delayBetweenAttempts)
        {
            this.applicationStateStore = applicationStateStore;
            this.dispatcher = dispatcher;
            this.messageStore = messageStore;
            this.maxAttempts = maxAttempts;
            this.delayBetweenAttempts = delayBetweenAttempts;
            this.localAddress = localAddress;

            var allHandlers = sideEffectsHandlers.Concat(new ISideEffectsHandler[]
            {
                new CompleteMessageSideEffectsHandler(messageStore),
                new WebMessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher)
            });

            processor = new ExactlyOnceProcessor<object>(allHandlers.ToArray(), new NServiceBusDebugLogger());
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            if (!context.MessageHeaders.ContainsKey(HumanInterfaceCompleteMessageHandler.TypeHeader))
            {
                await next().ConfigureAwait(false);
                return;
            }

            var (transactionId, partitionKey, attempt) = HumanInterfaceCompleteMessageHandler.Parse<TPartition>(context);

            if (!await messageStore.CheckExists(transactionId))
            {
                //The transaction has been completed
                return;
            }

            var transactionRecordContainer = applicationStateStore.Create(partitionKey);
            if (attempt < maxAttempts)
            {
                if (!await processor.TryApplySideEffects(transactionId, transactionRecordContainer).ConfigureAwait(false))
                {
                    //Try again later
                    await HumanInterfaceCompleteMessageHandler.Enqueue(transactionId, partitionKey, attempt + 1, delayBetweenAttempts, localAddress, dispatcher).ConfigureAwait(false);
                }
            }
            else
            {
                //Create tombstone record to force transaction timeout
                await processor.Process(transactionId, transactionRecordContainer, context, async (ctx, transactionContext) =>
                {
                    var messageExists = await messageStore.CheckExists(transactionId).ConfigureAwait(false);
                    if (!messageExists)
                    {
                        if (log.IsDebugEnabled)
                        {
                            log.Debug($"Ignoring duplicate message {transactionId} because the corresponding token no longer exists.");
                        }
                        return ProcessingResult<object>.Duplicate;
                    }
                    return ProcessingResult<object>.Successful(null);
                }).ConfigureAwait(false);
            }
        }
    }
}