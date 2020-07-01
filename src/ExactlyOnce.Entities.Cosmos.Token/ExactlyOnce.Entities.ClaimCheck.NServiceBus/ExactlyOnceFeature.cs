using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Extensibility;
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
                    var handlerCollection = new SideEffectsHandlerCollection(new ISideEffectsHandler[]
                    {
                        new MessagingWithClaimCheckSideEffectsHandler(settings.MessageStore, dispatcher) 
                    });
                    var processor = new ExactlyOnceProcessor<IExtendable>(settings.ApplicationStateStore, handlerCollection);
                    return new ExactlyOnceBehavior(new CorrelationManager(settings), processor, settings.MessageStore);
                }, 
                "Ensures side effects of processing messages by entities are persisted exactly once.");

            context.Pipeline.Register(new LoadMessageBodyBehavior(settings.MessageStore), "Loads message bodies from store.");
            context.Pipeline.Register(new CaptureOutgoingMessagesBehavior(settings.MessageStore), "Captures the outgoing messages.");
        }
    }
}