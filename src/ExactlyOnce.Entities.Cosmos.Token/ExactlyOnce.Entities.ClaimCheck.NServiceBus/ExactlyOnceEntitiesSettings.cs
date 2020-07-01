using System;
using System.Collections.Generic;
using ExactlyOnce.ClaimCheck;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public class ExactlyOnceEntitiesSettings
    {
        internal Dictionary<Type, Func<object, Dictionary<string, string>, string>> Mappers 
            = new Dictionary<Type, Func<object, Dictionary<string, string>, string>>();

        internal bool ProcessUnmappedMessages;
        internal IMessageStore MessageStore { get; set; }
        internal Microsoft.Azure.Cosmos.Container ApplicationStateStore { get; set; }

        public ExactlyOnceEntitiesSettings MapMessage<TMessage>(Func<TMessage, Dictionary<string, string>, string> correlationProperty)
        {
            Mappers[typeof(TMessage)] = (payload, headers) => correlationProperty((TMessage)payload, headers);
            return this;
        }

        public ExactlyOnceEntitiesSettings AllowProcessingUnmappedMessages()
        {
            ProcessUnmappedMessages = true;
            return this;
        }
    }
}