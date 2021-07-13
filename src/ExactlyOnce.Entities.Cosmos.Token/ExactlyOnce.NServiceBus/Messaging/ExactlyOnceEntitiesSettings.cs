using System;
using System.Collections.Generic;
using System.Net.Http;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Messaging;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public abstract class ExactlyOnceEntitiesSettings
    {
        internal IMessageStore MessageStore { get; set; }
        internal HttpClient HttpClient { get; set; }
        internal abstract ICorrelatedApplicationStateStore GetCorrelatedApplicationStateStore();
    }

    public class ExactlyOnceEntitiesSettings<T> : ExactlyOnceEntitiesSettings
    {
        readonly CorrelationManager<T> correlationManager = new CorrelationManager<T>();
        readonly IApplicationStateStore<T> applicationStateStore;

        internal ExactlyOnceEntitiesSettings(IApplicationStateStore<T> applicationStateStore)
        {
            this.applicationStateStore = applicationStateStore;
        }

        public ExactlyOnceEntitiesSettings<T> MapMessage<TMessage>(Func<TMessage, Dictionary<string, string>, T> correlationProperty)
        {
            correlationManager.MapMessage(correlationProperty);
            return this;
        }

        public ExactlyOnceEntitiesSettings<T> UseHttpClient(HttpClient client)
        {
            HttpClient = client;
            return this;
        }

        internal override ICorrelatedApplicationStateStore GetCorrelatedApplicationStateStore()
        {
            return new CorrelatedApplicationStateStore<T>(correlationManager, applicationStateStore);
        }
    }
}