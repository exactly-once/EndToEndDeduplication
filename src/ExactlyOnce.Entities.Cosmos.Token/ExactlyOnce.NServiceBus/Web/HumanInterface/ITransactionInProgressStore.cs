using System.Threading.Tasks;

namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    public interface ITransactionInProgressStore
    {
        Task BeginTransaction(string transactionId, string partitionKey);
        Task CompleteTransaction(string transactionId);
    }
}