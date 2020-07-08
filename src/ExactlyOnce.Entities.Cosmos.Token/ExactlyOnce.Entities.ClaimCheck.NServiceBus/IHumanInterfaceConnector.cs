using System;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    /// <summary>
    /// An instance of a connector
    /// </summary>
    public interface IHumanInterfaceConnector
    {
        /// <summary>
        /// Starts the connector.
        /// </summary>
        Task Start();

        /// <summary>
        /// Returns an NServiceBus session that can be used to send/publish messages using provided connection/transaction.
        /// </summary>
        Task ExecuteTransaction(string requestId, string partitionKey, Func<IHumanInterfaceConnectorMessageSession, Task> transaction);

        /// <summary>
        /// Stops the connector.
        /// </summary>
        Task Stop();
    }
}