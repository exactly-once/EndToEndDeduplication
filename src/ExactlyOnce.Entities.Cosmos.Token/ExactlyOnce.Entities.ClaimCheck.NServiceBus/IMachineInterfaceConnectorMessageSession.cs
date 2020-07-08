using System.IO;
using NServiceBus;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface IMachineInterfaceConnectorMessageSession : IMessageSession
    {
        /// <summary>
        /// Gets the transactional batch associated with this session.
        /// </summary>
        ITransactionBatchContext TransactionBatch { get; }

        /// <summary>
        /// Gets the body of the PUT request
        /// </summary>
        Stream PutRequestBody { get; }
    }
}