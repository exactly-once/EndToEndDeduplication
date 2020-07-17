using NServiceBus;

namespace Billing
{
    public class ProcessAuthorizeResponse : ICommand
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
    }
}