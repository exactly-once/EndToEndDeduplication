using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Blob;
using ExactlyOnce.NServiceBus.Cosmos;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.TransactionalSession.AcceptanceTests;
using NServiceBus.TransactionalSession.AcceptanceTests.Infrastructure;

namespace ExactlyOnce.AcceptanceTests.Infrastructure;

public class MachineInterfaceEndpoint : DefaultServer
{
    public override async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
        Action<EndpointConfiguration> configurationBuilderCustomization) =>
        await base.GetConfiguration(runDescriptor, endpointConfiguration, configuration =>
        {

            var messageStore = new MessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "acceptance-tests-claim-check"));
            var requestResponseStoreClient = new BlobContainerClient("UseDevelopmentStorage=true", "acceptance-tests-request-response");
            var clientOptions = new CosmosClientOptions();
            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
            var appDataContainer = client.GetContainer("ExactlyOnce.AcceptanceTests", "test-1");
            var stateStore = new ApplicationStateStore(appDataContainer, "PartitionKey");

            configuration.UseTokenBasedSessionWithDeduplication(stateStore, new MachineWebInterfaceRequestStore(requestResponseStoreClient, "request-"),
                new MachineWebInterfaceResponseStore(requestResponseStoreClient, "response-"), messageStore);

            configurationBuilderCustomization(configuration);
        });
}