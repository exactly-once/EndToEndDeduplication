using ExactlyOnce.AcceptanceTests.Infrastructure;

namespace NServiceBus.TransactionalSession.AcceptanceTests.Infrastructure;

public interface IChaosCollectionHolder
{
    public ChaosMonkeyCollection ChaosMonkeys { get; }
}