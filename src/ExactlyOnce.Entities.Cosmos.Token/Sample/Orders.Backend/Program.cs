using Azure.Storage.Blobs;
using Contracts;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Blob;
using ExactlyOnce.NServiceBus.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace Orders.Backend
{
    using System;
    using ExactlyOnce.NServiceBus.Testing;

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
            var appDataContainer = client.GetContainer("ExactlyOnce.Sample", "orders");

            var stateStore = new ApplicationStateStore(appDataContainer, "Customer");

            var chaosMonkey = new ChaosMonkey((id, allFailureModes) =>
            {
                var r = new Random(id.GetHashCode());
                return new[]
                {
                    r.Next(allFailureModes.Length),
                    r.Next(allFailureModes.Length),
                    r.Next(allFailureModes.Length)
                };
            });
            var messageHandlingChaosMonkey = new MessageHandlingChaosMonkey(chaosMonkey);
            var chaosStateStore = new TestingApplicationStateStore<string>(stateStore, messageHandlingChaosMonkey);
            var chaosMessageStore = new TestingMessageStore(messageStore, messageHandlingChaosMonkey);

            return Host.CreateDefaultBuilder(args)
                .UseNServiceBus(context =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.Orders.Backend");
                    endpointConfiguration.EnableInstallers();
                    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                    transport.ConnectionString("host=localhost");
                    transport.UseConventionalRoutingTopology();
                    var routing = transport.Routing();

                    var settings = endpointConfiguration.UseTokenBasedDeduplication(chaosStateStore, chaosMessageStore);
                    settings.MapMessage<BillingSucceeded>((message, headers) => message.CustomerId);
                    settings.MapMessage<BillingFailed>((message, headers) => message.CustomerId);

                    return endpointConfiguration;
                });
        }
    }
}
