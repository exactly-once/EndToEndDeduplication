using ExactlyOnce.Core;
using ExactlyOnce.NServiceBus.Web.HumanInterface;
using ExactlyOnce.NServiceBus.Web.MachineInterface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Transport;

// ReSharper disable once CheckNamespace
namespace ExactlyOnce.NServiceBus
{
    public static class ExactlyOnceHostBuilderExtensions
    {
        public static IHostBuilder UseAtomicCommitMessageSession<T>(this IHostBuilder hostBuilder, 
            IApplicationStateStore<T> applicationStateContainer,
            ITransactionInProgressStore<T> transactionInProgressStore,
            IMessageStore messageStore)
        {
            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                serviceCollection.AddHostedService<HumanInterfaceConnectorService<T>>();
                serviceCollection.AddSingleton<IHumanInterfaceConnector<T>>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var rootMessageSession = serviceProvider.GetService<IMessageSession>();

                    var connector = new HumanInterfaceConnector<T>(applicationStateContainer, new ISideEffectsHandler[0], 
                        rootMessageSession, dispatcher, transactionInProgressStore, messageStore);
                    return connector;
                });
            });


            return hostBuilder;
        }

        public static IHostBuilder UseExactlyOnceWebMachineInterface<T>(this IHostBuilder hostBuilder,
            IApplicationStateStore<T> applicationStateContainer,
            IMachineWebInterfaceRequestStore requestStore,
            IMachineWebInterfaceResponseStore responseStore,
            IMessageStore messageStore)
        {
            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                serviceCollection.AddSingleton<IMachineInterfaceConnector<T>>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var rootMessageSession = serviceProvider.GetService<IMessageSession>();
                    
                    var connector = new MachineInterfaceConnector<T>(
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