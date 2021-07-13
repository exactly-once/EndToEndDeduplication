using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Pipeline;

namespace ExactlyOnce.NServiceBus.Messaging
{
    class LoadMessageBodyBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        readonly IMessageStore messageStore;
        static readonly ILog log = LogManager.GetLogger<LoadMessageBodyBehavior>();

        public LoadMessageBodyBehavior(IMessageStore messageStore)
        {
            this.messageStore = messageStore;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            var messageBody = await messageStore.TryGet(context.MessageId).ConfigureAwait(false);
            if (messageBody == null)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"Ignoring duplicate message {context.MessageId} because the corresponding token no longer exists.");
                }
                return;
            }
            context.UpdateMessage(messageBody);

            await next().ConfigureAwait(false);
        }
    }
}