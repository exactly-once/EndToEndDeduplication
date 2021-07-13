namespace PaymentProvider.DomainModel
{
    public class Transaction
    {
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
    }
}