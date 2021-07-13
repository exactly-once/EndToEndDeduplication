using NServiceBus;

namespace Contracts
{
    public class BillingSucceeded : IMessage
    {
        public string CustomerId { get; set; }
        public string OrderId { get; set; }
    }
}