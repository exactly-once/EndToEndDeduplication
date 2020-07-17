using System;
using System.Collections.Generic;
using NServiceBus;

namespace Contracts
{
    public class BillCustomer : ICommand
    {
        public string CustomerId { get; set; }
        public string OrderId { get; set; }
        public List<OrderItem> Items { get; set; }
    }
}
