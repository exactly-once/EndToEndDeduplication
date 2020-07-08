using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface ITransactionContext
    {
        Guid AttemptId { get; }
        Task AddSideEffect(SideEffectRecord sideEffectRecord);
        Task AddSideEffects(List<SideEffectRecord> sideEffectRecords);
    }
}