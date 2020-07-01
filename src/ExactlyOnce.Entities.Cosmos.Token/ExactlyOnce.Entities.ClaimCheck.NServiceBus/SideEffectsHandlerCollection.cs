using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class SideEffectsHandlerCollection
    {
        readonly ISideEffectsHandler[] sideEffectsHandlers;

        public SideEffectsHandlerCollection(ISideEffectsHandler[] sideEffectsHandlers)
        {
            this.sideEffectsHandlers = sideEffectsHandlers;
        }

        public async Task Publish(string messageId, Guid attemptId, 
            IReadOnlyCollection<SideEffectRecord> committedSideEffects, 
            IReadOnlyCollection<SideEffectRecord> abortedSideEffects)
        {
            foreach (var handler in sideEffectsHandlers)
            {
                await handler.Publish(messageId, attemptId, committedSideEffects, abortedSideEffects).ConfigureAwait(false);
            }
        }
    }
}