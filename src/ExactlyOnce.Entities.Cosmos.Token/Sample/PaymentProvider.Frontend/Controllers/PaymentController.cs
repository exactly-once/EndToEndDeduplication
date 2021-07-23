using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;
using PaymentProvider.Contracts;
using PaymentProvider.DomainModel;
using PaymentProvider.Frontend.Models;

namespace PaymentProvider.Frontend.Controllers
{
    [ApiController]
    public class PaymentController : ControllerBase
    {
        readonly IMachineInterfaceConnectorMessageSession<AuthorizeRequest> session;

        public PaymentController(IMachineInterfaceConnectorMessageSession<AuthorizeRequest> session)
        {
            this.session = session;
        }

        [MachineInterface("payment/authorize/{transactionId}")]
        public async Task<IActionResult> Authorize(string transactionId)
        {
            var account = await session.TransactionContext.Batch()
                .TryReadItemAsync<Account>(session.Payload.CustomerId).ConfigureAwait(false)
                ?? new Account
                {
                    Number = session.Payload.CustomerId,
                    Partition = session.Payload.CustomerId.Substring(0, 2),
                    Balance = 0,
                    Transactions = new List<Transaction>()
                };

            account.Balance -= session.Payload.Amount;

            session.TransactionContext.Batch().UpsertItem(account);

            await session.Send(new SettleTransaction
            {
                AccountNumber = account.Number,
                Amount = session.Payload.Amount,
                TransactionId = transactionId
            });

            return new OkResult();
        }
    }
}
