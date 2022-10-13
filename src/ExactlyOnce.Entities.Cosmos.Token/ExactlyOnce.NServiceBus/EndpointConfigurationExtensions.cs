using System;
using ExactlyOnce.Core;
using ExactlyOnce.NServiceBus.Messaging;
using ExactlyOnce.NServiceBus.Messaging.MachineWebInterface;
using ExactlyOnce.NServiceBus.Web;
using ExactlyOnce.NServiceBus.Web.HumanInterface;
using ExactlyOnce.NServiceBus.Web.MachineInterface;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace ExactlyOnce.NServiceBus
{
    public static class EndpointConfigurationExtensions
    {
        public static ExactlyOnceEntitiesSettings<T> UseTokenBasedDeduplication<T>(this EndpointConfiguration config,
            IApplicationStateStore<T> applicationStateStore,
            IMessageStore messageStore)
        {
            if (!config.GetSettings().TryGet<ExactlyOnceEntitiesSettings<T>>("ExactlyOnce.Settings", out var settings))
            {
                settings = new ExactlyOnceEntitiesSettings<T>();
                config.GetSettings().Set("ExactlyOnce.Settings", settings);
            }
            settings.ApplicationStateStore = applicationStateStore;
            settings.MessageStore = messageStore;

            config.GetSettings().EnableFeatureByDefault(typeof(ExactlyOnceFeature));
            config.GetSettings().AddUnrecoverableException(typeof(ClientFaultHttpErrorException));
            return settings;
        }

        public static void UseTokenBasedSession<T>(this EndpointConfiguration config,
            IApplicationStateStore<T> applicationStateContainer,
            IMessageStore messageStore)
        {
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

            config.RegisterComponents(cc =>
            {
                cc.ConfigureComponent<IHumanInterfaceConnector<T>>(builder =>
                {
                    var dispatcher = builder.Build<IDispatchMessages>();
                    var rootMessageSession = builder.Build<SessionCaptureStartupTask>().Session;
                    var localAddressHolder = builder.Build<LocalAddressHolder>();

                    var connector = new HumanInterfaceConnector<T>(applicationStateContainer,
                        rootMessageSession, dispatcher, localAddressHolder.LocalAddress, messageStore, TimeSpan.FromSeconds(0));
                    return connector;
                }, DependencyLifecycle.SingleInstance);
            });

            config.GetSettings().EnableFeatureByDefault(typeof(SessionCaptureFeature));
        }

        public static void UseTokenBasedSessionWithDeduplication<T>(this EndpointConfiguration config, IApplicationStateStore<T> applicationStateContainer,
            IMachineWebInterfaceRequestStore requestStore,
            IMachineWebInterfaceResponseStore responseStore,
            IMessageStore messageStore)
        {
            config.RegisterComponents(cc =>
            {
                cc.ConfigureComponent<IMachineInterfaceConnector<T>>(builder =>
                {
                    var dispatcher = builder.Build<IDispatchMessages>();
                    var rootMessageSession = builder.Build<SessionCaptureStartupTask>().Session;

                    var connector = new MachineInterfaceConnector<T>(
                        applicationStateContainer,
                        Array.Empty<ISideEffectsHandler>(), //TODO: Add as parameter
                        rootMessageSession,
                        dispatcher,
                        messageStore,
                        requestStore,
                        responseStore);

                    return connector;
                }, DependencyLifecycle.SingleInstance);
            });

            config.GetSettings().EnableFeatureByDefault(typeof(SessionCaptureFeature));
        }
    }

    public class A
    {
    }
}