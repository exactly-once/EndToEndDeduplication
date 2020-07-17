using System;
using System.Net;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using Test.Shared;

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
            if (context.Request.Path.Value != "/add")
            {
                context.Response.StatusCode = 404;
                return;
            }

            var accountId = context.Request.Query["account"];
            var request = context.Request.Query["rid"];
            var change = int.Parse(context.Request.Query["change"]);
            var connector = context.RequestServices.GetRequiredService<IHumanInterfaceConnector<string>>();

            var code = await connector.ExecuteTransaction(request, accountId, async session =>
            {
                Account account;
                try
                {
                    account = await session.TransactionContext.Batch().ReadItemAsync<Account>(accountId);
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
                        return 500;
                    }
                }

                account.Value += change;
                session.TransactionContext.Batch().UpsertItem(account);
                await session.Send(new AddCommand
                {
                    AccountNumber = accountId,
                    Change = change
                });
                return 200;
            });

            context.Response.StatusCode = code;
        });
    }
}