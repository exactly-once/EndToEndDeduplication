using System;
using System.Linq;
using System.Net;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            if (context.Request.Path.Value != "/debit")
            {
                context.Response.StatusCode = 404;
                return;
            }

            var connector = context.RequestServices.GetRequiredService<IMachineInterfaceConnector>();
            
            if (string.Equals(context.Request.Method, "PUT", StringComparison.OrdinalIgnoreCase))
            {
                var requestId = context.Request.Query["rid"];
                await connector.StoreRequest(requestId, context.Request.Body);
                context.Response.StatusCode = 200;
            }
            else if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                var requestId = context.Request.Query["rid"];
                var accountId = context.Request.Query["account"];
                var response = await connector.ExecuteTransaction(requestId, accountId, async session =>
                {
                    var formReader = new FormReader(session.PutRequestBody);
                    var values = await formReader.ReadFormAsync().ConfigureAwait(false);
                    var formCollection = new FormCollection(values);

                    var debit = int.Parse(formCollection["debit-value"].Single());

                    Account account;
                    try
                    {
                        account = await session.TransactionBatch.ReadItemAsync<Account>(accountId);
                    }
                    catch (CosmosException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            account = new Account
                            {
                                Id = accountId,
                                AccountNumber = accountId
                            };
                        }
                        else
                        {
                            throw;
                        }
                    }

                    account.Value -= debit;
                    if (account.Value < 0)
                    {
                        return new StoredResponse(499, null);
                    }

                    session.TransactionBatch.UpsertItem(account);
                    return new StoredResponse(200, null);
                });
                context.Response.StatusCode = response.Code;
            }
            else if (string.Equals(context.Request.Method, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                var requestId = context.Request.Query["rid"];
                await connector.DeleteResponse(requestId);
                context.Response.StatusCode = 200;
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        });
    }
}