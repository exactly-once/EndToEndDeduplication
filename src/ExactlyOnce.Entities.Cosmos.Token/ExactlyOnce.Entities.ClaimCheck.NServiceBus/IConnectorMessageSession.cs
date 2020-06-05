using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using NServiceBus;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface IConnectorMessageSession 
    { 
        /// <summary>
        /// Gets the transactional batch associated with this session.
        /// </summary>
        TransactionalBatch TransactionBatch { get; }

        /// <summary>
        /// Application state container. Only for querying data.
        /// </summary>
        Container Container { get; }

        /// <summary>Sends the provided message.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        void Send(object message, SendOptions options = null);

        /// <summary>Publish the message to subscribers.</summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        void Publish(object message, PublishOptions options = null);
    }
}