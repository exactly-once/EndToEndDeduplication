using System.IO;
using ExactlyOnce.Core;
using NServiceBus;

// ReSharper disable once CheckNamespace
namespace ExactlyOnce.NServiceBus
{
    public interface IMachineInterfaceConnectorMessageSession<out TRequest> : IMessageSession
    {
        /// <summary>
        /// Gets the transaction context
        /// </summary>
        ITransactionContext TransactionContext { get; }

        /// <summary>
        /// Gets the body of the PUT request
        /// </summary>
        TRequest Payload { get; }
    }
}