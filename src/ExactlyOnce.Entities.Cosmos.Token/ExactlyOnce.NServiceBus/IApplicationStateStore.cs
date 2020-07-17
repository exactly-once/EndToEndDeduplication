using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus
{
    public interface IApplicationStateStore<in T>
    {
        ITransactionRecordContainer Create(T partitionKey);
    }
}