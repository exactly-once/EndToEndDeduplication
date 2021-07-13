﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using NServiceBus;
using PaymentProvider.Contracts;
using PaymentProvider.DomainModel;
using PaymentProvider.Frontend.Models;

namespace PaymentProvider.Frontend.Controllers
{
    [ApiController]
    public class PaymentController : ControllerBase
    {
        readonly ILogger<PaymentController> logger;
        readonly IMachineInterfaceConnector<string> connector;

        public PaymentController(ILogger<PaymentController> logger, IMachineInterfaceConnector<string> connector)
        {
            this.logger = logger;
            this.connector = connector;
        }

        [MachineInterface("payment/authorize/{transactionId}")]
        public async Task<IActionResult> AuthorizePost(string transactionId)
        {
            var result = await connector.ExecuteTransaction(transactionId,
                async payload =>
                {
                    var formReader = new FormReader(payload);
                    var values = await formReader.ReadFormAsync().ConfigureAwait(false);
                    var formCollection = new FormCollection(values);

                    var amount = decimal.Parse(formCollection["Amount"].Single());
                    var customerId = formCollection["CustomerId"].Single();

                    var partitionKey = customerId.Substring(0, 2);

                    return (new AuthorizeRequest
                    {
                        CustomerId = customerId,
                        TransactionId = transactionId,
                        Amount = amount
                    }, partitionKey);
                }, 
                async session =>
                {
                    var account = await LoadOrCreateAccount(session).ConfigureAwait(false);

                    account.Balance -= session.Payload.Amount;

                    session.TransactionContext.Batch().UpsertItem(account);

                    await session.Send(new SettleTransaction
                    {
                        AccountNumber = account.Number,
                        Amount = session.Payload.Amount,
                        TransactionId = transactionId
                    });

                    return new StoredResponse(200, null);
                });
            if (result.Body != null)
            {
                Response.Body = result.Body;
            }
            return StatusCode(result.Code);
        }

        static async Task<Account> LoadOrCreateAccount(IMachineInterfaceConnectorMessageSession<AuthorizeRequest> session)
        {
            Account account;
            try
            {
                account = await session.TransactionContext.Batch()
                    .ReadItemAsync<Account>(session.Payload.CustomerId);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    account = new Account
                    {
                        Number = session.Payload.CustomerId,
                        Partition = session.Payload.CustomerId.Substring(0, 2),
                        Balance = 0,
                        Transactions = new List<Transaction>()
                    };
                }
                else
                {
                    throw new Exception("Error while loading account data", e);
                }
            }

            return account;
        }
    }
}
