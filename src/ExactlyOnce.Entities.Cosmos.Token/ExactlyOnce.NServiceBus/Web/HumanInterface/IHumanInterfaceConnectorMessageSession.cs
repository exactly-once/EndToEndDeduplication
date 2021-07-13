using ExactlyOnce.Core;
using NServiceBus;

// ReSharper disable once CheckNamespace
namespace ExactlyOnce.NServiceBus
{
    public interface IHumanInterfaceConnectorMessageSession : IMessageSession
    {
        /// <summary>
        /// Gets the transaction context
        /// </summary>
        ITransactionContext TransactionContext { get; }
    }
}