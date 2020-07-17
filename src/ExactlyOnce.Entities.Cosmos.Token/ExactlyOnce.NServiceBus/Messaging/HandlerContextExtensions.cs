using ExactlyOnce.Core;

// Should be visible without having to add reference
// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public static class HandlerContextExtensions
    {
        public static ITransactionContext TransactionContext(this IMessageHandlerContext handlerContext)
        {
            return handlerContext.Extensions.Get<ITransactionContext>();
        }
    }
}