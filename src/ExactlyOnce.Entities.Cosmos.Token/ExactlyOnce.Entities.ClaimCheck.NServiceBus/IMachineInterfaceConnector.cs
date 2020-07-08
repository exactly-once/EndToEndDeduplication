using System;
using System.IO;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    /// <summary>
    /// An instance of a connector
    /// </summary>
    public interface IMachineInterfaceConnector
    {
        /// <summary>
        /// Starts the connector.
        /// </summary>
        Task Start();

        /// <summary>
        /// Returns an NServiceBus session that can be used to send/publish messages using provided connection/transaction.
        /// </summary>
        Task<StoredResponse> ExecuteTransaction(string requestId, string partitionKey, Func<IMachineInterfaceConnectorMessageSession, Task<StoredResponse>> transaction);

        /// <summary>
        /// Stores the PUT request body and created a deduplication token.
        /// </summary>
        Task StoreRequest(string requestId, Stream body);

        /// <summary>
        /// Removes the deduplication token and the stored response.
        /// </summary>
        Task DeleteResponse(string requestId);

        /// <summary>
        /// Stops the connector.
        /// </summary>
        Task Stop();
    }
}