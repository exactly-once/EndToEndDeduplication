using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;
using Orders.DomainModel;

namespace Orders.Backend
{
    public class BillingSucceededHandler : IHandleMessages<BillingSucceeded>
    {
        static ILog log = LogManager.GetLogger<BillingSucceededHandler>();

        public async Task Handle(BillingSucceeded message, IMessageHandlerContext context)
        {
            log.Info($"Marking order {message.OrderId} for customer {message.CustomerId} as billed successfully.");

            Order order = await context.TransactionContext().Batch().ReadItemAsync<Order>(message.OrderId);
            order.State = OrderState.Billed;

            context.TransactionContext().Batch().UpsertItem(order);
        }
    }
}