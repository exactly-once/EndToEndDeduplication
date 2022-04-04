using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingApplicationStateStore<T> : IApplicationStateStore<T>
    {
        IChaosMonkey chaosMonkey;
        IApplicationStateStore<T> impl;

        public TestingApplicationStateStore(IApplicationStateStore<T> impl, IChaosMonkey chaosMonkey)
        {
            this.chaosMonkey = chaosMonkey;
            this.impl = impl;
        }

        public ITransactionRecordContainer<T> Create(T partitionKey)
        {
            return new TestingTransactionRecordContainer<T>(impl.Create(partitionKey), chaosMonkey);
        }
    }
}