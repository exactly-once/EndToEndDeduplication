using NServiceBus;

namespace Contracts
{
    public class BillingFailed : IMessage
    {
        public string CustomerId { get; set; }
        public string OrderId { get; set; }
    }
}