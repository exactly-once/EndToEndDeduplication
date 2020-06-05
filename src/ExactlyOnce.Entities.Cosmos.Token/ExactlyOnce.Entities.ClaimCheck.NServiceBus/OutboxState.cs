using System.Collections.Generic;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class OutboxState
    {
        public OutboxState(string partitionId, string transactionId, List<OutboxRecord> records)
        {
            Records = records;
            PartitionId = partitionId;
            TransactionId = transactionId;
        }

        public List<OutboxRecord> Records { get; }
        public string TransactionId { get; }
        public string PartitionId { get; }
    }
}