using System.Collections.Generic;
using Newtonsoft.Json;

namespace PaymentProvider.DomainModel
{
    public class Account
    {
        [JsonProperty("id")]
        public string Number { get; set; }
        public string Partition { get; set; }
        public decimal Balance { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}