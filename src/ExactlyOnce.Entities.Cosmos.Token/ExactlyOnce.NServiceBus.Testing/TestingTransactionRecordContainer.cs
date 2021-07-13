using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingTransactionRecordContainer : ITransactionRecordContainer
    {
        ITransactionRecordContainer impl;

        public TestingTransactionRecordContainer(ITransactionRecordContainer impl)
        {
            this.impl = impl;
        }

        public string UniqueIdentifier => impl.UniqueIdentifier;

        public string MessageId => impl.MessageId;

        public Guid AttemptId => impl.AttemptId;

        public IReadOnlyCollection<SideEffectRecord> CommittedSideEffects => impl.CommittedSideEffects;

        public IReadOnlyCollection<SideEffectRecord> AbortedSideEffects => impl.AbortedSideEffects;

        public Task Load()
        {
            return impl.Load();
        }

        public Task AddSideEffect(SideEffectRecord sideEffectRecord)
        {
            return impl.AddSideEffect(sideEffectRecord);
        }

        public Task AddSideEffects(List<SideEffectRecord> sideEffectRecords)
        {
            return impl.AddSideEffects(sideEffectRecords);
        }

        public Task BeginStateTransition()
        {
            return impl.BeginStateTransition();
        }

        public Task CommitStateTransition(string messageId, Guid attemptId)
        {
            return impl.CommitStateTransition(messageId, attemptId);
        }

        public Task ClearTransactionState()
        {
            return impl.ClearTransactionState();
        }
    }
}