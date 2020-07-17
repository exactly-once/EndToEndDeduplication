using Azure.Storage.Blobs;
using Contracts;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Blob;
using ExactlyOnce.NServiceBus.Cosmos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace Orders
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
            var clientOptions = new CosmosClientOptions();
            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
            var appDataContainer = client.GetContainer("ExactlyOnce.Sample", "orders");
            var transactionInProgressContainer = client.GetContainer("ExactlyOnce.Sample", "transactions");

            var stateStore = new ApplicationStateStore(appDataContainer, "Customer");

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureServices(collection => collection.AddSingleton(appDataContainer))
                .UseNServiceBus(context =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.Orders");
                    var routing = endpointConfiguration.UseTransport<LearningTransport>().Routing();
                    routing.RouteToEndpoint(typeof(BillCustomer), "Samples.ExactlyOnce.Billing");
                    endpointConfiguration.SendOnly();
                    return endpointConfiguration;
                })
                .UseAtomicCommitMessageSession(stateStore, 
                    new TransactionInProgressStore(transactionInProgressContainer), messageStore);
        }
    }
}
