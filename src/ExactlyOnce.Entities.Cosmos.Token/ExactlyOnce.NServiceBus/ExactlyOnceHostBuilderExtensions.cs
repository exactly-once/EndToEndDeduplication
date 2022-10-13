using ExactlyOnce.NServiceBus.Web.MachineInterface;
using Microsoft.Extensions.Hosting;
using NServiceBus;

// ReSharper disable once CheckNamespace
namespace ExactlyOnce.NServiceBus
{
    using System;

    public static class ExactlyOnceHostBuilderExtensions
    {
        public static IHostBuilder UseNServiceBusWithAtomicSession<T>(this IHostBuilder hostBuilder,
            Func<HostBuilderContext, EndpointConfiguration> endpointConfigurationBuilder,
            IApplicationStateStore<T> applicationStateContainer,
            IMessageStore messageStore)
        {
            hostBuilder.UseNServiceBus(context =>
            {
                var config = endpointConfigurationBuilder(context);
                config.UseTokenBasedSession(applicationStateContainer, messageStore);

                return config;
            });
            
            return hostBuilder;
        }

        public static IHostBuilder UseNServiceBusWithExactlyOnceAtomicSession<T>(this IHostBuilder hostBuilder,
            Func<HostBuilderContext, EndpointConfiguration> endpointConfigurationBuilder,
            IApplicationStateStore<T> applicationStateContainer,
            IMachineWebInterfaceRequestStore requestStore,
            IMachineWebInterfaceResponseStore responseStore,
            IMessageStore messageStore)
        {
            hostBuilder.UseNServiceBus(context =>
            {
                var config = endpointConfigurationBuilder(context);
                config.UseTokenBasedSessionWithDeduplication(applicationStateContainer, requestStore, responseStore, messageStore);

                return config;
            });
            
            return hostBuilder;
        }
    }
}