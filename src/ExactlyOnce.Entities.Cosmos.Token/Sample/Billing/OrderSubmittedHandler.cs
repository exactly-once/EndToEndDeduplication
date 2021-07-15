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
    using System.Text;
    using Newtonsoft.Json;

    public class AuthorizeRequest
    {
        public string TransactionId { get; set; }
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
    }

    public class OrderSubmittedHandler : IHandleMessages<BillCustomer>
    {
        static ILog log = LogManager.GetLogger<OrderSubmittedHandler>();
        static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false);

        public async Task Handle(BillCustomer message, IMessageHandlerContext context)
        {
            var total = message.Items.Sum(x => x.Value);

            var request = new AuthorizeRequest
            {
                Amount = total,
                CustomerId = message.CustomerId
            };

            var content = CreateHttpContent(request);

            log.Info("Invoking payment provider API");

            await context.InvokeRest("http://localhost:57942/payment/authorize/{uniqueId}", content,
                new ProcessAuthorizeResponse
                {
                    CustomerId = message.CustomerId,
                    OrderId = message.OrderId
                });
        }

        static HttpContent CreateHttpContent(AuthorizeRequest request)
        {
            return new StringContent(JsonConvert.SerializeObject(request), Utf8Encoding, "application/json");
        }
    }
}