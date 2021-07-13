using System;
using System.Collections.Generic;
using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Messaging
{
    class CorrelatedApplicationStateStore<T> : ICorrelatedApplicationStateStore
    {
        readonly CorrelationManager<T> correlationManager;
        readonly IApplicationStateStore<T> applicationStateStore;

        public CorrelatedApplicationStateStore(CorrelationManager<T> correlationManager, IApplicationStateStore<T> applicationStateStore)
        {
            this.correlationManager = correlationManager;
            this.applicationStateStore = applicationStateStore;
        }

        public ITransactionRecordContainer Create(Type messageType, Dictionary<string, string> messageHeaders, object messageBody)
        {
            return applicationStateStore.Create(
                correlationManager.GetPartitionKey(messageType, messageHeaders, messageBody));
        }
    }
}