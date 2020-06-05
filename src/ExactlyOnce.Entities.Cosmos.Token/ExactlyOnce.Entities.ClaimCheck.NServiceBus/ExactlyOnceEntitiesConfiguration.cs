using Azure.Storage.Blobs;
using ExactlyOnce.ClaimCheck;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;

namespace ExactlyOnce.Cosmos
{
}

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public static class ExactlyOnceEntitiesConfiguration
    {
        public static ExactlyOnceEntitiesSettings UseExactlyOnce(this EndpointConfiguration config,
            Microsoft.Azure.Cosmos.Container applicationStateStore,
            IMessageStore messageStore, 
            IOutboxStore outboxStore)
        {
            var settings = config.GetSettings().GetOrCreate<ExactlyOnceEntitiesSettings>();
            settings.OutboxStore = outboxStore;
            settings.MessageStore = messageStore;
            settings.ApplicationStateStore = applicationStateStore;

            config.GetSettings().EnableFeatureByDefault(typeof(ExactlyOnceFeature));
            return settings;
        }

        
    }
}