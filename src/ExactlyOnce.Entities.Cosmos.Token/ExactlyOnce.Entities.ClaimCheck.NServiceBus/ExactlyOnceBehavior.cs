using System;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class ExactlyOnceBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        readonly CorrelationManager correlation;
        readonly ExactlyOnceProcessor<IExtendable> exactlyOnceProcessor;
        readonly IMessageStore messageStore;

        public ExactlyOnceBehavior(CorrelationManager correlation, ExactlyOnceProcessor<IExtendable> exactlyOnceProcessor, IMessageStore messageStore)
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
                //This message has not been mapped but correlation manager allows it to be processed.
                return next();
            }
            return exactlyOnceProcessor.Process(currentMessageId, partitionKey, context, async (ctx, batchContext, transactionContext) =>
            {
                //Check the de-duplication store if we already processed that message. Has to be done after loading the transaction.
                var messageExists = await messageStore.CheckExists(currentMessageId).ConfigureAwait(false);
                if (!messageExists)
                {
                    //Duplicate
                    return;
                }

                ctx.Extensions.Set(batchContext);
                ctx.Extensions.Set(transactionContext);
                await next().ConfigureAwait(false);
            });
        }

        
    }
}