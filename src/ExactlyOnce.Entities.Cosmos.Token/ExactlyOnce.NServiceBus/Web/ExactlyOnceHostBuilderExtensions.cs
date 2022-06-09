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
    using System;
    using global::NServiceBus.Settings;

    public static class ExactlyOnceHostBuilderExtensions
    {
        public static IHostBuilder UseNServiceBusAtomicWithSession<T>(this IHostBuilder hostBuilder,
            Func<HostBuilderContext, EndpointConfiguration> endpointConfigurationBuilder,
            IApplicationStateStore<T> applicationStateContainer,
            IMessageStore messageStore)
        {
            hostBuilder.UseNServiceBus(context =>
            {
                var config = endpointConfigurationBuilder(context);

                config.Pipeline.Register(b =>
                    {
                        var settings = b.Build<ReadOnlySettings>();
                        return new HumanInterfaceMessageSideEffectsBehavior<T>(applicationStateContainer,
                            Array.Empty<ISideEffectsHandler>(), //TODO: Add as parameter
                            b.Build<IDispatchMessages>(), messageStore, settings.LocalAddress(), 
                            6, //TODO: Add as parameter
                            TimeSpan.FromSeconds(10)); //TODO: Add as parameter
                    }, 
                    "Completes the human interface transactions.");

                return config;
            });

            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                serviceCollection.AddSingleton<IHumanInterfaceConnector<T>>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var rootMessageSession = serviceProvider.GetService<IMessageSession>();
                    var localAddressHolder = serviceProvider.GetService<LocalAddressHolder>();

                    var connector = new HumanInterfaceConnector<T>(applicationStateContainer,  
                        rootMessageSession, dispatcher, localAddressHolder.LocalAddress, messageStore, TimeSpan.FromSeconds(0));
                    return connector;
                });
            });


            return hostBuilder;
        }

        public static IHostBuilder UseNServiceBusWithExactlyOnceWebMachineInterface<T>(this IHostBuilder hostBuilder,
            Func<HostBuilderContext, EndpointConfiguration> endpointConfigurationBuilder,
            IApplicationStateStore<T> applicationStateContainer,
            IMachineWebInterfaceRequestStore requestStore,
            IMachineWebInterfaceResponseStore responseStore,
            IMessageStore messageStore)
        {
            hostBuilder.UseNServiceBus(endpointConfigurationBuilder);

            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                serviceCollection.AddSingleton<IMachineInterfaceConnector<T>>(serviceProvider =>
                {
                    var dispatcher = serviceProvider.GetService<IDispatchMessages>();
                    var rootMessageSession = serviceProvider.GetService<IMessageSession>();
                    
                    var connector = new MachineInterfaceConnector<T>(
                        applicationStateContainer, 
                        Array.Empty<ISideEffectsHandler>(), //TODO: Add as parameter
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