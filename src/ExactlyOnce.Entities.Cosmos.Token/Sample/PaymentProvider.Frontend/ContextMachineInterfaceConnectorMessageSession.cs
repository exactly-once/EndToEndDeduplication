namespace PaymentProvider.Frontend
{
    using System;
    using System.Threading.Tasks;
    using ExactlyOnce.Core;
    using ExactlyOnce.NServiceBus;
    using Microsoft.AspNetCore.Http;
    using NServiceBus;

    public class ContextMachineInterfaceConnectorMessageSession<T> : IMachineInterfaceConnectorMessageSession<T>
    {
        IHttpContextAccessor contextAccessor;

        public ContextMachineInterfaceConnectorMessageSession(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        IMachineInterfaceConnectorMessageSession<T> Impl => (IMachineInterfaceConnectorMessageSession<T>)contextAccessor.HttpContext.Items["session"];

        public Task Send(object message, SendOptions options)
        {
            return Impl.Send(message, options);
        }

        public Task Send<T1>(Action<T1> messageConstructor, SendOptions options)
        {
            return Impl.Send(messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return Impl.Publish(message, options);
        }

        public Task Publish<T1>(Action<T1> messageConstructor, PublishOptions publishOptions)
        {
            return Impl.Publish(messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return Impl.Subscribe(eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return Impl.Unsubscribe(eventType, options);
        }

        public ITransactionContext TransactionContext => Impl.TransactionContext;

        public T Payload => Impl.Payload;
    }
}