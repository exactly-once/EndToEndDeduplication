using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface IConnectorDeduplicationStore
    {
        /// <summary>
        /// Returns previous response if given request has already been processed. Otherwise returns null.
        /// </summary>
        /// <param name="requestId">Id of the request being processed.</param>
        /// <returns></returns>
        Task<ResponseMessage> HasBeenProcessed(string requestId);

        /// <summary>
        /// Marks given request as processed.
        /// </summary>
        /// <param name="requestId">Id of the request being processed.</param>
        /// <param name="response">The response generated for this request.</param>
        /// <returns></returns>
        Task MarkProcessed(string requestId, ResponseMessage response);
    }
}