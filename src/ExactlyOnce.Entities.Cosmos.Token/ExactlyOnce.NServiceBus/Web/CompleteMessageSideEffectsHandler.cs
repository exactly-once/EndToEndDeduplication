namespace ExactlyOnce.NServiceBus.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;

    /// <summary>
    /// Removes the tokens created for transactions that timed out
    /// </summary>
    class CompleteMessageSideEffectsHandler : SideEffectsHandler<CompleteTransactionMessageRecord>
    {
        readonly IMessageStore messageStore;

        public CompleteMessageSideEffectsHandler(IMessageStore messageStore)
        {
            this.messageStore = messageStore;
        }

        protected override async Task Publish(string messageId, Guid attemptId, IEnumerable<CompleteTransactionMessageRecord> committedSideEffects, IEnumerable<CompleteTransactionMessageRecord> abortedSideEffects)
        {
            var abortedIds = abortedSideEffects.Select(r => r.IncomingId).ToArray();
            await messageStore.EnsureDeleted(abortedIds)
                .ConfigureAwait(false);
        }
    }
}