using NServiceBus;

namespace PaymentProvider.Contracts
{
    public class SettleTransaction : ICommand
    {
        public string AccountNumber { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
    }
}