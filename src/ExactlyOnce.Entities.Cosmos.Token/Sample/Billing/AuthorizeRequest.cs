﻿namespace Billing
{
    public class AuthorizeRequest
    {
        public string TransactionId { get; set; }
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
    }
}