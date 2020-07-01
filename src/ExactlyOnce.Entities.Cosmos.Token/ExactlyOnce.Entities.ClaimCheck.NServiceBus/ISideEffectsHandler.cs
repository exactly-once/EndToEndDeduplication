using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface ISideEffectsHandler
    {
        Task Publish(string messageId, Guid attemptId, 
            IEnumerable<SideEffectRecord> committedSideEffects, IEnumerable<SideEffectRecord> abortedSideEffects);
    }

    public abstract class SideEffectsHandler<T> : ISideEffectsHandler
        where T : SideEffectRecord
    {
        Task ISideEffectsHandler.Publish(string messageId, Guid attemptId,
            IEnumerable<SideEffectRecord> committedSideEffects, IEnumerable<SideEffectRecord> abortedSideEffects)
        {
            return Publish(messageId, attemptId, committedSideEffects.OfType<T>(), abortedSideEffects.OfType<T>());
        }

        protected abstract Task Publish(string messageId, Guid attemptId,
            IEnumerable<T> committedSideEffects, IEnumerable<T> abortedSideEffects);
    }
}