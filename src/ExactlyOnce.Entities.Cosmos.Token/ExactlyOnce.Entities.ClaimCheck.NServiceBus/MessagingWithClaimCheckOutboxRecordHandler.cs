using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class MessagingWithClaimCheckSideEffectsHandler : SideEffectsHandler<OutgoingMessageRecord>
    {
        readonly IMessageStore messageStore;
        readonly IDispatchMessages dispatcher;

        public MessagingWithClaimCheckSideEffectsHandler(IMessageStore messageStore, IDispatchMessages dispatcher)
        {
            this.messageStore = messageStore;
            this.dispatcher = dispatcher;
        }

        protected override async Task Publish(string messageId, Guid attemptId, 
            IEnumerable<OutgoingMessageRecord> committedSideEffects, 
            IEnumerable<OutgoingMessageRecord> abortedSideEffects)
        {
            //Publish committed
            var operations = committedSideEffects.Select(r => r.ToTransportOperation()).ToArray();
            await dispatcher.Dispatch(new TransportOperations(operations), new TransportTransaction(), new ContextBag())
                .ConfigureAwait(false);

            //Clean up aborted
            await messageStore.EnsureDeleted(abortedSideEffects.Select(r => r.Id).ToArray())
                .ConfigureAwait(false);

            //Remove the incoming message
            await messageStore.Delete(messageId).ConfigureAwait(false);
        }
    }
}