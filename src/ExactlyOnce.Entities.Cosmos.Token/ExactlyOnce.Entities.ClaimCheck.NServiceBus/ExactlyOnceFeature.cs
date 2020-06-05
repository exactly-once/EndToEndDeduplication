using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Transport;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class ExactlyOnceFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<ExactlyOnceEntitiesSettings>();

            context.Pipeline.Register(b =>
                {
                    var dispatcher = b.Build<IDispatchMessages>();
                    var sideEffectsHandlers = new Dictionary<string, ISideEffectsHandler>
                    {
                        ["TransportOperation"] =
                            new MessagingWithClaimCheckSideEffectsHandler(settings.MessageStore, dispatcher),
                    };
                    var handlerCollection = new SideEffectsHandlerCollection(sideEffectsHandlers);
                    return new ExactlyOnceBehavior(settings.ApplicationStateStore, settings.OutboxStore, settings.MessageStore,
                        handlerCollection, new CorrelationManager(settings));
                }, 
                "Ensures side effects of processing messages by entities are persisted exactly once.");

            context.Pipeline.Register(new LoadMessageBodyBehavior(settings.MessageStore), "Loads message bodies from store.");
        }
    }
}