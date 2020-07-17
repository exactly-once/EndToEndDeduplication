using System;
using System.Net.Http;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace ExactlyOnce.NServiceBus.Messaging.MachineWebInterface
{
    class HttpClientBehavior : Behavior<IInvokeHandlerContext>
    {
        readonly HttpClient httpClient;

        public HttpClientBehavior(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public override Task Invoke(IInvokeHandlerContext context, Func<Task> next)
        {
            context.Extensions.Set(httpClient);
            return next();
        }
    }
}