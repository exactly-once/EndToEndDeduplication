namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    public class TransactionInProgress<TPartition>
    {
        public TransactionInProgress(string transactionId, TPartition partitionKey)
        {
            TransactionId = transactionId;
            PartitionKey = partitionKey;
        }

        public string TransactionId { get; }
        public TPartition PartitionKey { get; }
    }
}