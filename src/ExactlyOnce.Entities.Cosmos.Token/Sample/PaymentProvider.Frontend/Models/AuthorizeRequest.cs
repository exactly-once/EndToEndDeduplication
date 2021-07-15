namespace PaymentProvider.Frontend.Models
{
    public class AuthorizeRequest
    {
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
    }
}