using System.Collections.Generic;
using ExactlyOnce.ClaimCheck;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.Cosmos
{
    public static class ExactlyOnceHostBuilderExtensions
    {
        public static IHostBuilder UseExactlyOnce(this IHostBuilder hostBuilder, Microsoft.Azure.Cosmos.Container applicationStateStore, IOutboxStore outboxStore,
            IMessageStore messageStore, IConnectorDeduplicationStore deduplicationStore)
        {
            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                serviceCollection.AddSingleton<IHumanInterfaceConnector>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var sideEffectsHandlers = new Dictionary<string, ISideEffectsHandler>
                    {
                        ["TransportOperation"] =
                            new MessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher),
                        ["HttpResponse"] =
                            new DeduplicationStoreResponseSideEffectsHandler(deduplicationStore)
                    };

                    var connector = new HumanInterfaceConnector(applicationStateStore, outboxStore,
                        deduplicationStore, serviceProvider.GetService<IMessageSession>());
                    return connector;
                });
            });


            return hostBuilder;
        }
    }
}