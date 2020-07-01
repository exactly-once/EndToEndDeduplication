using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface ITransactionInProgressStore
    {
        Task BeginTransaction(string transactionId, string partitionKey);
        Task CompleteTransaction(string transactionId);
    }
}