using Azure.Storage.Blobs;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Blob;
using ExactlyOnce.NServiceBus.Cosmos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using PaymentProvider.Contracts;

namespace PaymentProvider.Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var messageStore = new MessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "claim-check"));
            var requestResponseStoreClient = new BlobContainerClient("UseDevelopmentStorage=true", "payment-provider-web");

            var clientOptions = new CosmosClientOptions();
            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
            var appDataContainer = client.GetContainer("ExactlyOnce.Sample", "payment-provider");

            var stateStore = new ApplicationStateStore(appDataContainer, "Partition");

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(collection => collection.AddSingleton(appDataContainer))
                .UseNServiceBus(context =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.PaymentProvider.Frontend");
                    endpointConfiguration.SendOnly();
                    var routing = endpointConfiguration.UseTransport<LearningTransport>().Routing();
                    routing.RouteToEndpoint(typeof(SettleTransaction), "Samples.ExactlyOnce.PaymentProvider.Backend");
                    return endpointConfiguration;
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseExactlyOnceWebMachineInterface(stateStore,
                    new MachineWebInterfaceRequestStore(requestResponseStoreClient, "request-"),
                    new MachineWebInterfaceResponseStore(requestResponseStoreClient, "response-"), messageStore);
        }
    }
}
