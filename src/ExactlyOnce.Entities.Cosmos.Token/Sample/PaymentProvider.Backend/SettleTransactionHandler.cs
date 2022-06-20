using System.Threading.Tasks;
using Microsoft.Extensions.Azure;
using NServiceBus;
using NServiceBus.Logging;
using PaymentProvider.Contracts;
using PaymentProvider.DomainModel;

namespace PaymentProvider.Backend
{
    public class SettleTransactionHandler : IHandleMessages<SettleTransaction>
    {
        static ILog log = LogManager.GetLogger<SettleTransactionHandler>();

        public async Task Handle(SettleTransaction message, IMessageHandlerContext context)
        {
            log.Info($"Settling transaction {message.TransactionId}");

            Account account = await context.TransactionBatch().ReadItemAsync<Account>(message.AccountNumber);
            account.Transactions.Add(new Transaction
            {
                Amount = message.Amount,
                TransactionId = message.TransactionId
            });

            context.TransactionBatch().UpsertItem(account);
        }
    }
}