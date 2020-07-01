using System;
using System.Net;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
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
            if (context.Request.Path.Value != "/")
            {
                context.Response.StatusCode = 404;
                return;
            }
            var partitionKey = context.Request.Query["account"];
            var request = context.Request.Query["rid"];
            var change = int.Parse(context.Request.Query["change"]);

            var connector = context.RequestServices.GetRequiredService<IConnector>();

            await connector.ExecuteTransaction(request, partitionKey, async session =>
            {
                Account account;
                try
                {
                    account = await session.TransactionBatch.ReadItemAsync<Account>(partitionKey);
                }
                catch (CosmosException e)
                {
                    if (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        account = new Account
                        {
                            Id = partitionKey,
                            AccountNumber = partitionKey
                        };
                    }
                    else
                    {
                        throw;
                    }
                }

                account.Value += change;
                session.TransactionBatch.UpsertItem(account);
                await session.Send(new AddCommand
                {
                    AccountNumber = partitionKey,
                    Change = change
                });
            });

            context.Response.StatusCode = 200;
        });
        //app.UseRouting();
        //app.UseEndpoints(c => c.MapControllers());
    }
}