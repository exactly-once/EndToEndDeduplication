using System;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentProvider.Frontend
{
    using NServiceBus;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HumanInterfaceAttribute : Attribute, IFilterFactory
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new Filter(PartitionKey, serviceProvider.GetRequiredService<IHumanInterfaceConnector<string>>());
        }

        /// <inheritdoc />
        public string Name { get; set; }
        public string PartitionKey { get; set; }
        public bool IsReusable => true;

        class Filter : IAsyncActionFilter
        {
            readonly string partitionKey;
            readonly IHumanInterfaceConnector<string> connector;

            public Filter(string partitionKey, IHumanInterfaceConnector<string> connector)
            {
                this.partitionKey = partitionKey;
                this.connector = connector;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var partitionKeyValue = (string)context.RouteData.Values[partitionKey];

                await connector.ExecuteTransaction(partitionKeyValue,
                    async session =>
                    {
                        context.HttpContext.Items.Add("session", session);
                        context.HttpContext.Items.Add("batch", session.TransactionContext.Batch());

                        await next().ConfigureAwait(false);

                        return true;
                    });
            }
        }

    }
}