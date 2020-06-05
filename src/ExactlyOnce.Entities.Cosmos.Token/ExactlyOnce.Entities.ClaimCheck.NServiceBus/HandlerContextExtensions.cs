using ExactlyOnce.Entities.ClaimCheck.NServiceBus;

// Should be visible without having to add reference
// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public static class HandlerContextExtensions
    {
        public static ITransactionBatchContext TransactionBatch(this IMessageHandlerContext handlerContext)
        {
            return handlerContext.Extensions.Get<ITransactionBatchContext>();
        }
    }
}