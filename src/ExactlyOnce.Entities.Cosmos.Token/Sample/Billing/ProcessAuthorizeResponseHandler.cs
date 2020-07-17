using System.Net;
using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;

namespace Billing
{
    public class ProcessAuthorizeResponseHandler : IHandleMessages<ProcessAuthorizeResponse>
    {
        static ILog log = LogManager.GetLogger<ProcessAuthorizeResponseHandler>();

        public Task Handle(ProcessAuthorizeResponse message, IMessageHandlerContext context)
        {
            log.Info("Processing payment provider response");

            var response = context.GetResponse();
            if (response.Status == HttpStatusCode.BadRequest) //No funds
            {
                log.Info("Authorization failed");
                return context.Send(new BillingFailed
                {
                    CustomerId = message.CustomerId,
                    OrderId = message.OrderId
                });
            }

            log.Info("Authorization succeeded");
            return context.Send(new BillingSucceeded
            {
                CustomerId = message.CustomerId,
                OrderId = message.OrderId
            });
        }
    }
}