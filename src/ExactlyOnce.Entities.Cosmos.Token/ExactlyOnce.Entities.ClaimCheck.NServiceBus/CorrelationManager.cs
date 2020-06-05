using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using NServiceBus;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class CorrelationManager
    {
        Dictionary<Type, Func<object, Dictionary<string, string>, string>> mappers;
        bool processUnmappedMessages;

        public CorrelationManager(ExactlyOnceEntitiesSettings settings)
        {
            mappers = settings.Mappers;
            processUnmappedMessages = settings.ProcessUnmappedMessages;
        }

        public bool TryGetPartitionKey(Type messageType, Dictionary<string, string> messageHeaders, object messageBody, out string key)
        {
            if (!mappers.TryGetValue(messageType, out var mapper))
            {
                if (!processUnmappedMessages)
                {
                    throw new Exception($"Message of type {messageType.FullName} has been rejected by this endpoint because it has not been mapped to a partition.");
                }
                key = default;
                return false;
            }
            key = mapper(messageBody, messageHeaders);
            return true;
        }
    }
}