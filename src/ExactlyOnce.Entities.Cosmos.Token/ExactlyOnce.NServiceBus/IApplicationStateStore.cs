using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus
{
    public interface IApplicationStateStore<T>
    {
        ITransactionRecordContainer<T> Create(T partitionKey);
    }
}