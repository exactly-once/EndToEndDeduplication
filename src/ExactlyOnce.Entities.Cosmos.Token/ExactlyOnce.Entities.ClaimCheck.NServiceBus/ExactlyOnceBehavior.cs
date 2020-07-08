using System;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class ExactlyOnceBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        readonly CorrelationManager correlation;
        readonly ExactlyOnceProcessor<IExtendable, object> exactlyOnceProcessor;
        readonly IMessageStore messageStore;

        static readonly ILog log = LogManager.GetLogger<ExactlyOnceBehavior>();

        public ExactlyOnceBehavior(CorrelationManager correlation, ExactlyOnceProcessor<IExtendable, object> exactlyOnceProcessor, IMessageStore messageStore)
        {
            this.correlation = correlation;
            this.exactlyOnceProcessor = exactlyOnceProcessor;
            this.messageStore = messageStore;
        }

        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            var currentMessageId = context.MessageId;

            if (!correlation.TryGetPartitionKey(context.Message.MessageType, context.Headers, context.Message.Instance,
                out var partitionKey))
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"Message {currentMessageId} has not been mapped to exactly-once partition.");
                }
                //This message has not been mapped but correlation manager allows it to be processed.
                return next();
            }
            if (log.IsDebugEnabled)
            {
                log.Debug($"Beginning exactly-once processing of message {currentMessageId}.");
            }
            return exactlyOnceProcessor.Process(currentMessageId, partitionKey, context, async (ctx, batchContext, transactionContext) =>
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

                ctx.Extensions.Set(batchContext);
                ctx.Extensions.Set(transactionContext);
                await next().ConfigureAwait(false);

                return ProcessingResult<object>.Successful(null);
            });
        }

        
    }
}