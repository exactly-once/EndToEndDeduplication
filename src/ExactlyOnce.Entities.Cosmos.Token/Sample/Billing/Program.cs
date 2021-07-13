using Azure.Storage.Blobs;
using Contracts;
using ExactlyOnce.NServiceBus.Blob;
using ExactlyOnce.NServiceBus.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace Billing
{
    class Program
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
            var appDataContainer = client.GetContainer("ExactlyOnce.Sample", "billing");

            var stateStore = new ApplicationStateStore(appDataContainer, "Customer");

            return Host.CreateDefaultBuilder(args)
                .UseNServiceBus(context =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.Billing");
                    var routing = endpointConfiguration.UseTransport<LearningTransport>().Routing();

                    routing.RouteToEndpoint(typeof(BillingSucceeded), "Samples.ExactlyOnce.Orders.Backend");
                    routing.RouteToEndpoint(typeof(BillingFailed), "Samples.ExactlyOnce.Orders.Backend");
                    routing.RouteToEndpoint(typeof(ProcessAuthorizeResponse), "Samples.ExactlyOnce.Billing");

                    var settings = endpointConfiguration.UseExactlyOnce(stateStore, messageStore);
                    settings.MapMessage<BillCustomer>((message, headers) => message.CustomerId);
                    settings.MapMessage<ProcessAuthorizeResponse>((message, headers) => message.CustomerId);

                    return endpointConfiguration;
                });
        }
    }
}
