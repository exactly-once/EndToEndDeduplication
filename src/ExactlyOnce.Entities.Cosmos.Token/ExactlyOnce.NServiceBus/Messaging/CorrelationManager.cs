using System;
using System.Collections.Generic;

namespace ExactlyOnce.NServiceBus.Messaging
{
    class CorrelationManager<T>
    {
        readonly Dictionary<Type, Func<object, Dictionary<string, string>, T>> messageMappers
            = new Dictionary<Type, Func<object, Dictionary<string, string>, T>>();

        public void MapMessage<TMessage>(Func<TMessage, Dictionary<string, string>, T> correlationProperty)
        {
            messageMappers[typeof(TMessage)] = (payload, headers) => correlationProperty((TMessage)payload, headers);
        }

        public T GetPartitionKey(Type messageType, Dictionary<string, string> messageHeaders, object messageBody)
        {
            if (!messageMappers.TryGetValue(messageType, out var mapper))
            {
                throw new Exception($"Message of type {messageType.FullName} has been rejected by this endpoint because it has not been mapped to a partition.");
            }
            var key = mapper(messageBody, messageHeaders);
            return key;
        }
    }
}