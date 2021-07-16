using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentProvider.Frontend
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class MachineInterfaceAttribute : Attribute, IFilterFactory, IActionHttpMethodProvider, IRouteTemplateProvider
    {
        static readonly IEnumerable<string> SupportedMethods = new[] { "PUT", "POST", "DELETE" };

        int? order;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new Filter(serviceProvider.GetRequiredService<IMachineInterfaceConnector<string>>());
        }

        /// <summary>
        /// Creates a new <see cref="RouteAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public MachineInterfaceAttribute(string template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
        }

        /// <inheritdoc />
        public string Template { get; }

        /// <summary>
        /// Gets the route order. The order determines the order of route execution. Routes with a lower order
        /// value are tried first. If an action defines a route by providing an <see cref="IRouteTemplateProvider"/>
        /// with a non <c>null</c> order, that order is used instead of this value. If neither the action nor the
        /// controller defines an order, a default value of 0 is used.
        /// </summary>
        public int Order
        {
            get => order ?? 0;
            set => order = value;
        }

        /// <inheritdoc />
        int? IRouteTemplateProvider.Order => order;

        /// <inheritdoc />
        public string Name { get; set; }

        public bool IsReusable => true;
        public IEnumerable<string> HttpMethods => SupportedMethods;

        class Filter : IAsyncActionFilter
        {
            readonly IMachineInterfaceConnector<string> connector;

            public Filter(IMachineInterfaceConnector<string> connector)
            {
                this.connector = connector;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (string.Equals(context.HttpContext.Request.Method, "put", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionId = (string)context.RouteData.Values["transactionId"];
                    await connector.StoreRequest(transactionId, context.HttpContext.Request.BodyReader.AsStream()).ConfigureAwait(false);

                    context.Result = new OkResult();
                    return;
                }

                if (string.Equals(context.HttpContext.Request.Method, "delete", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionId = (string)context.RouteData.Values["transactionId"];
                    await connector.DeleteResponse(transactionId).ConfigureAwait(false);

                    context.Result = new OkResult();
                    return;
                }

                await next().ConfigureAwait(false);
            }
        }

    }
}