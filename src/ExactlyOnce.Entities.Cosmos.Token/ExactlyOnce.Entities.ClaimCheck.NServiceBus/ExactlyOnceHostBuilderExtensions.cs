using System.Collections.Generic;
using Azure.Storage.Blobs;
using ExactlyOnce.ClaimCheck;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.Cosmos
{
    public static class ExactlyOnceHostBuilderExtensions
    {
        public static IHostBuilder UseAtomicCommitMessageSession(this IHostBuilder hostBuilder, 
            Container applicationStateContainer,
            Container transactionInProgressContainer,
            IMessageStore messageStore)
        {
            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                serviceCollection.AddSingleton<IHumanInterfaceConnector>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var rootMessageSession = serviceProvider.GetService<IMessageSession>();

                    var transactionInProgressStore = new TransactionInProgressStore(transactionInProgressContainer);
                    
                    var connector = new HumanInterfaceConnector(applicationStateContainer, new ISideEffectsHandler[0], 
                        rootMessageSession, dispatcher, transactionInProgressStore, messageStore);
                    return connector;
                });
            });


            return hostBuilder;
        }

        public static IHostBuilder UseExactlyOnceWebMachineInterface(this IHostBuilder hostBuilder,
            Container applicationStateContainer,
            IMachineWebInterfaceRequestStore requestStore,
            IMachineWebInterfaceResponseStore responseStore,
            IMessageStore messageStore)
        {
            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                serviceCollection.AddSingleton<IMachineInterfaceConnector>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var rootMessageSession = serviceProvider.GetService<IMessageSession>();
                    
                    var connector = new MachineInterfaceConnector(
                        applicationStateContainer, 
                        new ISideEffectsHandler[0], 
                        rootMessageSession, 
                        dispatcher, 
                        messageStore, 
                        requestStore, 
                        responseStore);

                    return connector;
                });
            });


            return hostBuilder;
        }
    }
}