using System;
using System.Threading.Tasks;
using ExactlyOnce.Core;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Pipeline;

namespace ExactlyOnce.NServiceBus.Messaging
{
    class ExactlyOnceBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        readonly ICorrelatedApplicationStateStore applicationStateStore;
        readonly ExactlyOnceProcessor<IExtendable> exactlyOnceProcessor;
        readonly IMessageStore messageStore;

        static readonly ILog log = LogManager.GetLogger<ExactlyOnceBehavior>();

        public ExactlyOnceBehavior(ExactlyOnceProcessor<IExtendable> exactlyOnceProcessor, IMessageStore messageStore, ICorrelatedApplicationStateStore applicationStateStore)
        {
            this.exactlyOnceProcessor = exactlyOnceProcessor;
            this.messageStore = messageStore;
            this.applicationStateStore = applicationStateStore;
        }

        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            var currentMessageId = context.MessageId;
            if (log.IsDebugEnabled)
            {
                log.Debug($"Beginning exactly-once processing of message {currentMessageId}.");
            }

            var transactionRecordContainer = applicationStateStore.Create(context.Message.MessageType, context.Headers, context.Message.Instance);

            return exactlyOnceProcessor.Process(currentMessageId, transactionRecordContainer, context, async (ctx, transactionContext) =>
            {
                var messageExists = await messageStore.CheckExists(currentMessageId).ConfigureAwait(false);
                if (!messageExists)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug($"Ignoring duplicate message {context.MessageId} because the corresponding token no longer exists.");
                    }
                    return ProcessingResult<object>.Duplicate;
                }

                ctx.Extensions.Set(transactionContext);
                await next().ConfigureAwait(false);

                return ProcessingResult<object>.Successful(null);
            });
        }

        
    }
}