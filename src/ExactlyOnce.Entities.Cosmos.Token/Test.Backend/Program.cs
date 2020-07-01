using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.ClaimCheck.BlobStore;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using Test.Shared;

namespace Test.Backend
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var messageStore = new BlobMessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "claim-check"));

            var clientOptions = new CosmosClientOptions();
            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
            var appDataContainer = client.GetContainer("ExactlyOnce", "backend");

            var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.Backend");
            endpointConfiguration.UseTransport<LearningTransport>();
            var exactlyOnceSettings =  endpointConfiguration.UseExactlyOnce(appDataContainer, messageStore);
            exactlyOnceSettings.MapMessage<AddCommand>((payload, headers) => payload.AccountNumber);

            endpointConfiguration.Recoverability().Immediate(i => i.NumberOfRetries(0));
            endpointConfiguration.Recoverability().Delayed(d => d.NumberOfRetries(0));
            
            var endpoint = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();

            await endpoint.Stop();
        }
    }
}

