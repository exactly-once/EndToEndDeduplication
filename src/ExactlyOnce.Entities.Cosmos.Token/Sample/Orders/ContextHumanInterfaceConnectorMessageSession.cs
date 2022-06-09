namespace PaymentProvider.Frontend
{
    using System;
    using System.Threading.Tasks;
    using ExactlyOnce.Core;
    using ExactlyOnce.NServiceBus;
    using Microsoft.AspNetCore.Http;
    using NServiceBus;

    public class ContextHumanInterfaceConnectorMessageSession : IHumanInterfaceConnectorMessageSession
    {
        readonly IHttpContextAccessor contextAccessor;

        public ContextHumanInterfaceConnectorMessageSession(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        IHumanInterfaceConnectorMessageSession Impl => (IHumanInterfaceConnectorMessageSession)contextAccessor.HttpContext.Items["session"];

        public async Task Send(object message, SendOptions options)
        {
            await Impl.Send(message, options);
        }

        public async Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            await Impl.Send(messageConstructor, options);
        }

        public async Task Publish(object message, PublishOptions options)
        {
            await Impl.Publish(message, options);
        }

        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            await Impl.Publish(messageConstructor, publishOptions);
        }

        public async Task Subscribe(Type eventType, SubscribeOptions options)
        {
            await Impl.Subscribe(eventType, options);
        }

        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            await Impl.Unsubscribe(eventType, options);
        }

        public ITransactionContext TransactionContext => Impl.TransactionContext;
    }
}