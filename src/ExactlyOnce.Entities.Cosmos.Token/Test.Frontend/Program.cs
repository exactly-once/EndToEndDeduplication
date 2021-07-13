using System.Linq;
using Azure.Storage.Blobs;
using ExactlyOnce.NServiceBus.Blob;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Test.Shared;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Cosmos;

public class Program
{
    public static void Main()
    {
        var messageStore = new MessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "claim-check"));
        var clientOptions = new CosmosClientOptions();
        var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
        var appDataContainer = client.GetContainer("ExactlyOnce", "accounts");
        var transactionInProgressContainer = client.GetContainer("ExactlyOnce", "transactions");

        var stateStore = new ApplicationStateStore(appDataContainer);

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(c => c.UseStartup<Startup>())
            .UseNServiceBus(context =>
            {
                var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.Frontend");
                var transport = endpointConfiguration.UseTransport<LearningTransport>();
                transport.Routing().RouteToEndpoint(
                    assembly: typeof(AddCommand).Assembly,
                    destination: "Samples.ExactlyOnce.Backend");

                endpointConfiguration.SendOnly();

                return endpointConfiguration;
            })
            .UseAtomicCommitMessageSession(stateStore, new TransactionInProgressStore(transactionInProgressContainer), messageStore)
            .Build();

        host.Run();
    }
}
