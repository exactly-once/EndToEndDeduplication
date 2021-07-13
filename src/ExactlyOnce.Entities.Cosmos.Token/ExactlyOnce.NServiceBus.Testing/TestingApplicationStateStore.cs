using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingApplicationStateStore<T> : IApplicationStateStore<T>
    {
        IApplicationStateStore<T> impl;
        public ITransactionRecordContainer Create(T partitionKey)
        {
            return impl.Create(partitionKey);
        }
    }
}