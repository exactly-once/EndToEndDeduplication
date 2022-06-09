using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace ExactlyOnce.NServiceBus
{
    /// <summary>
    /// An instance of a connector
    /// </summary>
    public interface IHumanInterfaceConnector<in TPartition>
    {
        /// <summary>
        /// Returns an NServiceBus session that can be used to send/publish messages using provided connection/transaction.
        /// </summary>
        Task<T> ExecuteTransaction<T>(TPartition partitionKey, Func<IHumanInterfaceConnectorMessageSession, Task<T>> transaction);
    }
}