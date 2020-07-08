using Azure.Storage.Blobs;
using ExactlyOnce.ClaimCheck.BlobStore;
using ExactlyOnce.Cosmos;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Test.Shared;

public class Program
{
    public static void Main()
    {
        var clientOptions = new CosmosClientOptions();
        var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
        var appDataContainer = client.GetContainer("ExactlyOnce", "external");

        #region EndpointConfiguration

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(c => c.UseStartup<Startup>())
            .UseNServiceBus(context =>
            {
                var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.ExternalService");
                endpointConfiguration.UseTransport<LearningTransport>();
                endpointConfiguration.SendOnly();
                return endpointConfiguration;
            })
            .UseExactlyOnceWebMachineInterface(appDataContainer, 
                new MachineWebInterfaceBlobRequestStore(new BlobContainerClient("UseDevelopmentStorage=true", "request-store"), "external-service-"),
                new MachineWebInterfaceBlobResponseStore(new BlobContainerClient("UseDevelopmentStorage=true", "response-store"), "external-service-"),
                new BlobMessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "claim-check")))
            .Build();

        #endregion

        host.Run();
    }
}
