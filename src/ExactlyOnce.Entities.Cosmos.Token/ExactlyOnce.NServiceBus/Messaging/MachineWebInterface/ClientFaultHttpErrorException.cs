using System;

namespace ExactlyOnce.NServiceBus.Messaging.MachineWebInterface
{
    public class ClientFaultHttpErrorException : Exception
    {
        public ClientFaultHttpErrorException(string message)
            : base(message)
        {
        }
    }
}