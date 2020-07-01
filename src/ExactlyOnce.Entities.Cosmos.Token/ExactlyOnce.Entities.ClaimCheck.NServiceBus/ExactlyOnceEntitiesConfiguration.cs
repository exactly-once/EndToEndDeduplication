using Azure.Storage.Blobs;
using ExactlyOnce.ClaimCheck;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public static class ExactlyOnceEntitiesConfiguration
    {
        public static ExactlyOnceEntitiesSettings UseExactlyOnce(this EndpointConfiguration config,
            Microsoft.Azure.Cosmos.Container applicationStateStore,
            IMessageStore messageStore)
        {
            var settings = config.GetSettings().GetOrCreate<ExactlyOnceEntitiesSettings>();
            settings.MessageStore = messageStore;
            settings.ApplicationStateStore = applicationStateStore;

            config.GetSettings().EnableFeatureByDefault(typeof(ExactlyOnceFeature));
            return settings;
        }

        
    }
}