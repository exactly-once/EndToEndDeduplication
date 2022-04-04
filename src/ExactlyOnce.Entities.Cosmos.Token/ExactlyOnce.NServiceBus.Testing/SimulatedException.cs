namespace ExactlyOnce.NServiceBus.Testing
{
    using System;

    public class SimulatedException : Exception
    {
        public SimulatedException(string message) : base(message)
        {
        }
    }
}