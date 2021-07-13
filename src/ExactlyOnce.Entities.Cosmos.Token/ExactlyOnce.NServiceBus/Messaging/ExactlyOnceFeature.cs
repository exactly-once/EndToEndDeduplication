using System.Net.Http;
using ExactlyOnce.Core;
using ExactlyOnce.NServiceBus.Messaging.MachineWebInterface;
using ExactlyOnce.NServiceBus.Web.MachineInterface;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Transport;

namespace ExactlyOnce.NServiceBus.Messaging
{
    class ExactlyOnceFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<ExactlyOnceEntitiesSettings>("ExactlyOnce.Settings");
            var httpClient = settings.HttpClient ?? new HttpClient();

            context.Pipeline.Register(b =>
                {
                    var dispatcher = b.Build<IDispatchMessages>();
                    var handlerCollection = new ISideEffectsHandler[]
                    {
                        new MessagingWithClaimCheckSideEffectsHandler(settings.MessageStore, dispatcher),
                        new MachineInterfaceInvocationSideEffectsHandler(httpClient), 
                    };
                    var processor = new ExactlyOnceProcessor<IExtendable>(handlerCollection, new NServiceBusDebugLogger());
                    return new ExactlyOnceBehavior(processor, settings.MessageStore, settings.GetCorrelatedApplicationStateStore());
                }, 
                "Ensures side effects of processing messages by entities are persisted exactly once.");

            context.Pipeline.Register(new CaptureOutgoingMessagesBehavior(settings.MessageStore), "Captures the outgoing messages.");

            context.Pipeline.Register(new LoadMessageBodyBehavior(settings.MessageStore), "Loads message bodies from store.");

            context.Pipeline.Register(new HttpClientBehavior(httpClient), "Adds HttpClient to the context.");
            context.Pipeline.Register(new MachineInterfaceFollowUpMessageHandlingBehavior(httpClient), "Issues the POST and DELETE requests in the machine interface");
        }
    }
}