using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;
using Orders.DomainModel;

namespace Orders.Backend
{
    public class BillingFailedHandler : IHandleMessages<BillingFailed>
    {
        static ILog log = LogManager.GetLogger<BillingFailedHandler>();

        public async Task Handle(BillingFailed message, IMessageHandlerContext context)
        {
            log.Info($"Marking order {message.OrderId} for customer {message.CustomerId} as failed billing.");

            Order order = await context.TransactionBatch().ReadItemAsync<Order>(message.OrderId);
            order.State = OrderState.BillingFailed;

            context.TransactionBatch().UpsertItem(order);
        }
    }
}