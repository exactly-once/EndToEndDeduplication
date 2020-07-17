using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.NServiceBus.Blob;
using ExactlyOnce.NServiceBus.Cosmos;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using Test.Shared;

namespace Test.Backend
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var messageStore = new MessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "claim-check"));

            var clientOptions = new CosmosClientOptions();
            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
            var appDataContainer = client.GetContainer("ExactlyOnce", "backend");

            var stateStore = new ApplicationStateStore(appDataContainer);
           
            var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.Backend");
            var transport = endpointConfiguration.UseTransport<LearningTransport>();
            
            var settings = endpointConfiguration.UseExactlyOnce(stateStore, messageStore);
            settings.MapMessage<AddCommand>((payload, headers) => payload.AccountNumber);
            settings.MapMessage<DebitCommand>((payload, headers) => payload.AccountNumber);
            settings.MapMessage<DebitCompleteCommand>((payload, headers) => payload.AccountNumber);

            transport.Routing().RouteToEndpoint(typeof(DebitCompleteCommand), "Samples.ExactlyOnce.Backend");

            endpointConfiguration.Recoverability().Delayed(d => d.NumberOfRetries(0));
            
            var endpoint = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();

            await endpoint.Stop();
        }
    }
}

