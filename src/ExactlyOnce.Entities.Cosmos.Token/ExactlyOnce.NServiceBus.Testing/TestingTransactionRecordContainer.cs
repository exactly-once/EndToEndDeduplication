using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingTransactionRecordContainer<T> : ITransactionRecordContainer<T>
    {
        ITransactionRecordContainer<T> impl;
        readonly IChaosMonkey chaosMonkey;

        public TestingTransactionRecordContainer(ITransactionRecordContainer<T> impl, IChaosMonkey chaosMonkey)
        {
            this.impl = impl;
            this.chaosMonkey = chaosMonkey;
        }

        public T UniqueIdentifier => impl.UniqueIdentifier;

        public object Unwrap()
        {
            return impl;
        }

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
            chaosMonkey.AddSideEffect(UniqueIdentifier.ToString());
            return impl.AddSideEffect(sideEffectRecord);
        }

        public Task AddSideEffects(List<SideEffectRecord> sideEffectRecords)
        {
            chaosMonkey.AddSideEffect(UniqueIdentifier.ToString());
            return impl.AddSideEffects(sideEffectRecords);
        }

        public Task BeginStateTransition()
        {
            chaosMonkey.BeginStateTransition(UniqueIdentifier.ToString());
            return impl.BeginStateTransition();
        }

        public Task CommitStateTransition(string messageId, Guid attemptId)
        {
            chaosMonkey.CommitStateTransition(UniqueIdentifier.ToString());
            return impl.CommitStateTransition(messageId, attemptId);
        }

        public Task ClearTransactionState()
        {
            chaosMonkey.ClearTransactionState(UniqueIdentifier.ToString());
            return impl.ClearTransactionState();
        }
    }
}