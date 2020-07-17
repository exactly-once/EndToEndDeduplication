using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using Test.Shared;

namespace Test.Backend
{
    public class AddCommandHandler : IHandleMessages<AddCommand>
    {
        public async Task Handle(AddCommand message, IMessageHandlerContext context)
        {
            Account account;
            try
            {
                account = await context.TransactionContext().Batch().ReadItemAsync<Account>(message.AccountNumber);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    account = new Account
                    {
                        Id = message.AccountNumber,
                        AccountNumber = message.AccountNumber
                    };
                }
                else
                {
                    throw;
                }
            }

            account.Value += message.Change;
            context.TransactionContext().Batch().UpsertItem(account);
            await context.SendLocal(new DebitCommand
            {
                AccountNumber = message.AccountNumber,
                Change = message.Change
            }).ConfigureAwait(false);
        }
    }
}