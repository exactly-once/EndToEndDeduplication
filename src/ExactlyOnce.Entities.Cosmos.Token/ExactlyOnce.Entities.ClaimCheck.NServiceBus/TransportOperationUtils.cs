using System;
using System.Collections.Generic;
using ExactlyOnce.ClaimCheck;
using NServiceBus;
using NServiceBus.DelayedDelivery;
using NServiceBus.DeliveryConstraints;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    static class TransportOperationUtils
    {
        public static OutgoingMessageRecord ToMessageRecord(this TransportOperation op, string incomingId, Guid attemptId)
        {
            return new OutgoingMessageRecord
            {
                Id = op.Message.MessageId,
                AttemptId = attemptId,
                IncomingId = incomingId,
                Headers = op.Message.Headers,
                Metadata = SerializeOptions(op)
            };
        }

        public static TransportOperation ToTransportOperation(this OutgoingMessageRecord message)
        {
            return new TransportOperation(new OutgoingMessage(message.Id, message.Headers, new byte[0]),
                DeserializeAddressTag(message.Metadata),
                DispatchConsistency.Default,
                DeserializeConstraints(message.Metadata));
        }

        public static Message ToCheck(this TransportOperation op, Guid attemptId)
        {
            return new Message(op.Message.MessageId, op.Message.Body);
        }


        static Dictionary<string, string> SerializeOptions(TransportOperation operation)
        {
            var options = new Dictionary<string, string>
            {
                ["MessageId"] = operation.Message.MessageId
            };
            foreach (var constraint in operation.DeliveryConstraints)
            {
                SerializeDeliveryConstraint(constraint, options);
            }

            SerializeAddressTag(operation.AddressTag, options);
            return options;
        }

        static void SerializeAddressTag(AddressTag addressTag, Dictionary<string, string> options)
        {
            if (addressTag is MulticastAddressTag indirect)
            {
                options["EventType"] = indirect.MessageType.AssemblyQualifiedName;
                return;
            }

            if (addressTag is UnicastAddressTag direct)
            {
                options["Destination"] = direct.Destination;
                return;
            }

            throw new Exception($"Unknown routing strategy {addressTag.GetType().FullName}");
        }

        static void SerializeDeliveryConstraint(DeliveryConstraint constraint, Dictionary<string, string> options)
        {
            if (constraint is NonDurableDelivery)
            {
                options["NonDurable"] = true.ToString();
                return;
            }
            if (constraint is DoNotDeliverBefore doNotDeliverBefore)
            {
                options["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(doNotDeliverBefore.At);
                return;
            }

            if (constraint is DelayDeliveryWith delayDeliveryWith)
            {
                options["DelayDeliveryFor"] = delayDeliveryWith.Delay.ToString();
                return;
            }

            if (constraint is DiscardIfNotReceivedBefore discard)
            {
                options["TimeToBeReceived"] = discard.MaxTime.ToString();
                return;
            }

            throw new Exception($"Unknown delivery constraint {constraint.GetType().FullName}");
        }

        static List<DeliveryConstraint> DeserializeConstraints(Dictionary<string, string> options)
        {
            var constraints = new List<DeliveryConstraint>(4);
            if (options.ContainsKey("NonDurable"))
            {
                constraints.Add(new NonDurableDelivery());
            }

            if (options.TryGetValue("DeliverAt", out var deliverAt))
            {
                constraints.Add(new DoNotDeliverBefore(DateTimeExtensions.ToUtcDateTime(deliverAt)));
            }

            if (options.TryGetValue("DelayDeliveryFor", out var delay))
            {
                constraints.Add(new DelayDeliveryWith(TimeSpan.Parse(delay)));
            }

            if (options.TryGetValue("TimeToBeReceived", out var ttbr))
            {
                constraints.Add(new DiscardIfNotReceivedBefore(TimeSpan.Parse(ttbr)));
            }
            return constraints;
        }

        static AddressTag DeserializeAddressTag(Dictionary<string, string> options)
        {
            if (options.TryGetValue("Destination", out var destination))
            {
                return new UnicastAddressTag(destination);
            }

            if (options.TryGetValue("EventType", out var eventType))
            {
                return new MulticastAddressTag(Type.GetType(eventType, true));
            }

            throw new Exception("Could not find routing strategy to deserialize");
        }
    }
}