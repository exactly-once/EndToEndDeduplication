using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Core
{
    public interface ITransactionRecordContainer
    {
        string UniqueIdentifier { get; }
        string MessageId { get; }
        Guid AttemptId { get; }
        IReadOnlyCollection<SideEffectRecord> CommittedSideEffects { get; }
        IReadOnlyCollection<SideEffectRecord> AbortedSideEffects { get; }

        Task Load();
        Task AddSideEffect(SideEffectRecord sideEffectRecord);
        Task AddSideEffects(List<SideEffectRecord> sideEffectRecords);
        Task BeginStateTransition();
        Task CommitStateTransition(string messageId, Guid attemptId);
        Task ClearTransactionState();
    }
}