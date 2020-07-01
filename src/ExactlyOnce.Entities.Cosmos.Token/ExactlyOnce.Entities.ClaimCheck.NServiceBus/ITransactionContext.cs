using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface ITransactionContext
    {
        Guid AttemptId { get; }
        Task AddSideEffects(List<SideEffectRecord> messageRecords);
    }
}