using System;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using NServiceBus;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class WebCaptureOutgoingMessagesBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        readonly IMessageStore messageStore;

        public WebCaptureOutgoingMessagesBehavior(IMessageStore messageStore)
        {
            this.messageStore = messageStore;
        }

        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            var transaction = context.Extensions.Get<ITransactionContext>();

            var pendingOperations = new PendingTransportOperations();
            context.Extensions.Set(pendingOperations);

            await next().ConfigureAwait(false);

            var messageRecords = pendingOperations.Operations.Select(o => TransportOperationUtils.ToMessageRecord(o, context.MessageId, transaction.AttemptId)).Cast<SideEffectRecord>().ToList();
            var messagesToCheck = pendingOperations.Operations.Select(o => o.ToCheck(transaction.AttemptId)).ToArray();

            await transaction.AddSideEffects(messageRecords).ConfigureAwait(false);
            await messageStore.Create(messagesToCheck).ConfigureAwait(false);
        }
    }
}