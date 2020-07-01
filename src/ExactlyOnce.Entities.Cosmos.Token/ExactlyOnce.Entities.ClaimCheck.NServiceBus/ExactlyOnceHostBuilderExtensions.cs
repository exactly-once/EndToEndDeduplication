using System.Collections.Generic;
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
                serviceCollection.AddSingleton<IConnector>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var transactionInProgressStore = new TransactionInProgressStore(transactionInProgressContainer);
                    var sideEffectsHandlers = new ISideEffectsHandler[]
                    {
                        new WebMessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher),
                        new TransactionInProgressSideEffectHandler(transactionInProgressStore),
                    };

                    var connector = new HumanInterfaceConnector(applicationStateContainer, new SideEffectsHandlerCollection(sideEffectsHandlers),
                        serviceProvider.GetService<IMessageSession>(), transactionInProgressStore, messageStore);
                    return connector;
                });
            });


            return hostBuilder;
        }
    }
}