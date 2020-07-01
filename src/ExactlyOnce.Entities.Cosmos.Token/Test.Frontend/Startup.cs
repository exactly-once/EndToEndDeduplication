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
            var partition = context.Request.Query["account"];
            var request = context.Request.Query["rid"];
            var change = int.Parse(context.Request.Query["change"]);
            var partitionKey = new PartitionKey(partition);

            var connector = context.RequestServices.GetRequiredService<IHumanInterfaceConnector>();

            await connector.ExecuteTransaction(request, partition, context.Response, async session =>
            {
                Account account;
                try
                {
                    account = await session.Container.ReadItemAsync<Account>(partition, partitionKey);
                }
                catch (CosmosException e)
                {
                    if (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        account = new Account
                        {
                            Id = partition,
                            AccountNumber = partition
                        };
                    }
                    else
                    {
                        throw;
                    }
                }

                account.Value += change;
                session.TransactionBatch.UpsertItem(account);
                session.Send(new AddCommand
                {
                    AccountNumber = partition,
                    Change = change
                });
                return 200;
            });
        });
        //app.UseRouting();
        //app.UseEndpoints(c => c.MapControllers());
    }
}