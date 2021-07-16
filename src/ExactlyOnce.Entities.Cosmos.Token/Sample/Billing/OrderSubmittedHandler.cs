using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;

namespace Billing
{
    public class OrderSubmittedHandler : IHandleMessages<BillCustomer>
    {
        static ILog log = LogManager.GetLogger<OrderSubmittedHandler>();

        public async Task Handle(BillCustomer message, IMessageHandlerContext context)
        {
            var total = message.Items.Sum(x => x.Value);

            var request = new AuthorizeRequest
            {
                Amount = total,
                CustomerId = message.CustomerId
            };

            log.Info("Invoking payment provider API");

            await context.InvokeRest("http://localhost:57942/payment/authorize/{uniqueId}", request,
                new ProcessAuthorizeResponse
                {
                    CustomerId = message.CustomerId,
                    OrderId = message.OrderId
                });
        }

        
    }
}