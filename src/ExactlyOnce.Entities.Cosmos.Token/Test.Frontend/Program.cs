using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.ClaimCheck.BlobStore;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Test.Shared;
using ResponseMessage = Microsoft.Azure.Cosmos.ResponseMessage;

public class Program
{
    public static void Main()
    {
        var messageStore = new BlobMessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "claim-check"));
        var deduplicationStore = new BlobConnectorDeduplicationStore(new BlobContainerClient("UseDevelopmentStorage=true", "deduplication"));
        var outboxStore = new BlobOutboxStore(new BlobContainerClient("UseDevelopmentStorage=true", "outbox"));

        var clientOptions = new CosmosClientOptions();
        var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
        var appDataContainer = client.GetContainer("ExactlyOnce", "accounts");

        #region EndpointConfiguration

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
            .UseExactlyOnce(appDataContainer, outboxStore, messageStore, deduplicationStore)
            .Build();

        #endregion

        host.Run();
    }
}
