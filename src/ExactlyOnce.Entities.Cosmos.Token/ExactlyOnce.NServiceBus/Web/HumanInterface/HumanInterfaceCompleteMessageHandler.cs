namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NServiceBus.DelayedDelivery;
    using global::NServiceBus.DeliveryConstraints;
    using global::NServiceBus.Extensibility;
    using global::NServiceBus.Pipeline;
    using global::NServiceBus.Routing;
    using global::NServiceBus.Transport;

    class HumanInterfaceCompleteMessageHandler
    {
        public const string TypeHeader = "ExactlyOnce.NServiceBus.DispatchMessage";
        const string PartitionKeyHeader = "ExactlyOnce.NServiceBus.DispatchMessage.PartitionKey";
        const string TransactionIdHeader = "ExactlyOnce.NServiceBus.DispatchMessage.TransactionId";
        const string AttemptHeader = "ExactlyOnce.NServiceBus.DispatchMessage.Attempt";

        public static (string transactionId, TPartition partitionKey, int attempt) Parse<TPartition>(IIncomingPhysicalMessageContext context)
        {
            var partitionKeyString = context.MessageHeaders[PartitionKeyHeader];
            var partitionKey = (TPartition)Convert.ChangeType(partitionKeyString, typeof(TPartition));
            var attemptString = context.MessageHeaders[AttemptHeader];
            var transactionId = context.MessageHeaders[TransactionIdHeader];
            var attempt = int.Parse(attemptString);

            return (transactionId, partitionKey, attempt);
        }

        public static Task Enqueue<TPartition>(string transactionId, TPartition partitionKey, int attempt, TimeSpan delay, string localAddress, IDispatchMessages dispatcher)
        {
            var headers = new Dictionary<string, string>
            {
                { TypeHeader, "true" },
                { AttemptHeader, attempt.ToString() },
                { PartitionKeyHeader, Convert.ToString(partitionKey) },
                { TransactionIdHeader, transactionId }
            };
            var deliveryConstraints = new List<DeliveryConstraint>();
            if (delay != TimeSpan.Zero)
            {
                deliveryConstraints.Add(new DelayDeliveryWith(delay));
            }
            var operation = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), headers, Array.Empty<byte>()), new UnicastAddressTag(localAddress), DispatchConsistency.Isolated, deliveryConstraints);
            return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
        }
    }
}