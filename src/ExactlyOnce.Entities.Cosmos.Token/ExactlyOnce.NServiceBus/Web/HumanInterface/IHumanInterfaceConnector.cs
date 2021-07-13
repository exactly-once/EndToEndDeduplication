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
        /// Starts the connector.
        /// </summary>
        Task Start();

        /// <summary>
        /// Returns an NServiceBus session that can be used to send/publish messages using provided connection/transaction.
        /// </summary>
        Task<TResult> ExecuteTransaction<TResult>(string requestId, TPartition partitionKey, Func<IHumanInterfaceConnectorMessageSession, Task<TResult>> transaction);

        /// <summary>
        /// Stops the connector.
        /// </summary>
        Task Stop();
    }
}