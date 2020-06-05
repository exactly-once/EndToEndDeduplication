using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    /// <summary>
    /// An instance of a connector
    /// </summary>
    public interface IConnector
    {
        /// <summary>
        /// Starts the connector.
        /// </summary>
        Task Start();

        /// <summary>
        /// Returns an NServiceBus session that can be used to send/publish messages using provided connection/transaction.
        /// </summary>
        Task ExecuteTransaction(string requestId, string partitionKey, HttpResponse currentResponse, 
            Func<IConnectorMessageSession, Task<int>> transaction);

        /// <summary>
        /// Stops the connector.
        /// </summary>
        Task Stop();
    }
}