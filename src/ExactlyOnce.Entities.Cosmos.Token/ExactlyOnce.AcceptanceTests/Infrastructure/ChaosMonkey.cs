using System;
using System.Threading;

namespace ExactlyOnce.AcceptanceTests.Infrastructure;

public class ChaosMonkey
{
    readonly int failuresBeforeSuccess;
    int failures;

    public ChaosMonkey(int failuresBeforeSuccess)
    {
        this.failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public void Consult()
    {
        var incremented = Interlocked.Increment(ref failures);
        if (incremented <= failuresBeforeSuccess)
        {
            throw new Exception("Simulated!");
        }
    }

    public override string ToString()
    {
        return $@"Failures before success {failuresBeforeSuccess}";
    }
}

