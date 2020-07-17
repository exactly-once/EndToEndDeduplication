using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Core
{
    public interface ITransactionContext
    {
        Guid AttemptId { get; }
        Task AddSideEffect(SideEffectRecord sideEffectRecord);
        Task AddSideEffects(List<SideEffectRecord> sideEffectRecords);
        ITransactionRecordContainer TransactionRecordContainer { get; }
    }
}