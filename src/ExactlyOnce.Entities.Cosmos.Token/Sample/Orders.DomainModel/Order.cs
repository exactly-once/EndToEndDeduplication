using System.Collections.Generic;
using Newtonsoft.Json;

namespace Orders.DomainModel
{
    public class Order
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string Customer { get; set; } //Partition key

        public List<Item> Items { get; set; } = new List<Item>();

        public OrderState State { get; set; }
    }
}