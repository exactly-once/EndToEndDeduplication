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
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using PaymentProvider.Frontend;

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

            var stateStore = new ApplicationStateStore(appDataContainer, "Customer");

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureServices(collection =>
                {
                    collection.AddSingleton(appDataContainer);
                    collection.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    collection.AddSingleton<IHumanInterfaceConnectorMessageSession, ContextHumanInterfaceConnectorMessageSession>();
                })
                .UseNServiceBusWithAtomicSession(context =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.Orders");
                    endpointConfiguration.EnableInstallers();
                    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                    transport.ConnectionString("host=localhost");
                    transport.UseConventionalRoutingTopology();
                    var routing = transport.Routing();
                    routing.RouteToEndpoint(typeof(BillCustomer), "Samples.ExactlyOnce.Billing");
                    return endpointConfiguration;
                }, stateStore, messageStore);
        }
    }
}
