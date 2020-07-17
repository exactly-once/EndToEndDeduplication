using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Messaging;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public static class ExactlyOnceEntitiesConfiguration
    {
        public static ExactlyOnceEntitiesSettings<T> UseExactlyOnce<T>(this EndpointConfiguration config,
            IApplicationStateStore<T> applicationStateStore,
            IMessageStore messageStore)
        {
            if (!config.GetSettings().TryGet<ExactlyOnceEntitiesSettings<T>>("ExactlyOnce.Settings", out var settings))
            {
                settings = new ExactlyOnceEntitiesSettings<T>(applicationStateStore);
                config.GetSettings().Set("ExactlyOnce.Settings", settings);
            }
            settings.MessageStore = messageStore;

            config.GetSettings().EnableFeatureByDefault(typeof(ExactlyOnceFeature));
            return settings;
        }
    }
}