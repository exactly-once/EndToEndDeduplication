using NServiceBus;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface IHumanInterfaceConnectorMessageSession : IMessageSession
    {
        /// <summary>
        /// Gets the transactional batch associated with this session.
        /// </summary>
        ITransactionBatchContext TransactionBatch { get; }
    }
}