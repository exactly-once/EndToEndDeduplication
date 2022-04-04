namespace Tests
{
    using System;
    using ExactlyOnce.NServiceBus.Testing;
    using NUnit.Framework;

    [TestFixture]
    public class ChaosMonkeyTests
    {
        [Test]
        public void It_does_not_throw_exception()
        {
            var monkey = new ChaosMonkey((s, failureModes) => new int[]{});
            monkey.InvokeChaos("A", MessageHandlingFailureMode.AddSideEffect);
        }

        [Test]
        public void It_throws_exception()
        {
            var monkey = new ChaosMonkey((s, failureModes) => new[] { (int)MessageHandlingFailureMode.AddSideEffect });

            Assert.Throws<Exception>(() => monkey.InvokeChaos("A", MessageHandlingFailureMode.AddSideEffect));
        }

        [Test]
        public void It_cleans_up_after_last_step()
        {
            var monkey = new ChaosMonkey((s, failureModes) => new[] { (int)MessageHandlingFailureMode.AddSideEffect });

            Assert.Throws<Exception>(() => monkey.InvokeChaos("A", MessageHandlingFailureMode.AddSideEffect));

            //Does not throw because we completed all failures
            monkey.InvokeChaos("A", MessageHandlingFailureMode.AddSideEffect);

            //Completes the tracker
            monkey.InvokeChaos("A", MessageHandlingFailureMode.EnsureMessageDeleted);

            //Throws because a new tracker is created
            Assert.Throws<Exception>(() => monkey.InvokeChaos("A", MessageHandlingFailureMode.AddSideEffect));
        }
    }
}
