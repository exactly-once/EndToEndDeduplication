using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class MessagingWithClaimCheckSideEffectsHandler : ISideEffectsHandler
    {
        readonly IMessageStore messageStore;
        readonly IDispatchMessages dispatcher;

        public MessagingWithClaimCheckSideEffectsHandler(IMessageStore messageStore, IDispatchMessages dispatcher)
        {
            this.messageStore = messageStore;
            this.dispatcher = dispatcher;
        }

        public Task Prepare(List<OutboxRecord> records)
        {
            var messages = records.Select(r => r.ToClaimCheckMessage()).ToArray();
            return messageStore.Create(messages);
        }

        public Task Commit(List<OutboxRecord> records)
        {
            var operations = records.Select(r => r.ToClaimCheckTransportOperation()).ToArray();
            return dispatcher.Dispatch(new TransportOperations(operations), new TransportTransaction(), new ContextBag());
        }
    }
}