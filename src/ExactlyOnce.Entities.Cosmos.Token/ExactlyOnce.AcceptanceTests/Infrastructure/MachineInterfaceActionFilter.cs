using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ExactlyOnce.AcceptanceTests.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class MachineInterfaceAttribute : Attribute, IFilterFactory, IActionHttpMethodProvider
    {
        static readonly IEnumerable<string> SupportedMethods = new[] { "PUT", "POST", "DELETE" };

        int? order;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new Filter(TransactionId, PartitionKey, serviceProvider.GetRequiredService<IMachineInterfaceConnector<string>>());
        }

        public string PartitionKey { get; set; }
        public string TransactionId { get; set; }

        public bool IsReusable => true;
        public IEnumerable<string> HttpMethods => SupportedMethods;

        class Filter : IAsyncResourceFilter
        {
            readonly string transactionIdProp;
            readonly string partitionKeyProp;
            readonly IMachineInterfaceConnector<string> connector;

            public Filter(string transactionIdProp, string partitionKeyProp, IMachineInterfaceConnector<string> connector)
            {
                this.transactionIdProp = transactionIdProp;
                this.partitionKeyProp = partitionKeyProp;
                this.connector = connector;
            }

            public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
            {
                if (string.Equals(context.HttpContext.Request.Method, "put", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionId = (string)context.RouteData.Values[transactionIdProp];
                    await connector.StoreRequest(transactionId, context.HttpContext.Request.BodyReader.AsStream()).ConfigureAwait(false);
                    context.Result = new OkResult();
                    return;
                }

                if (string.Equals(context.HttpContext.Request.Method, "delete", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionId = (string)context.RouteData.Values[transactionIdProp];
                    await connector.DeleteResponse(transactionId).ConfigureAwait(false);

                    context.Result = new OkResult();
                    return;
                }

                if (string.Equals(context.HttpContext.Request.Method, "post", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionId = (string)context.RouteData.Values[transactionIdProp];
                    var patritionKey = (string)context.RouteData.Values[partitionKeyProp];

                    try
                    {
                        var responseStream = context.HttpContext.Response.Body;
                        StoredResponse result;
                        using (var capturedResponse = new MemoryStream())
                        {
                            context.HttpContext.Response.Body = capturedResponse;

                            result = await connector.ExecuteTransaction(transactionId, patritionKey, async session =>
                            {
                                context.HttpContext.Items.Add("session", session);

                                context.HttpContext.Request.Body = session.Payload;
                                context.HttpContext.Request.ContentLength = 20; //TODO !!!

                                var resultContext = await next().ConfigureAwait(false);

                                if (resultContext.HttpContext.Response.StatusCode <= 300 && resultContext.Exception == null)
                                {
                                    capturedResponse.Seek(0, SeekOrigin.Begin);
                                    //Store the successful response
                                    return new StoredResponse(resultContext.HttpContext.Response.StatusCode, capturedResponse);
                                }

                                //Force retry of non 2xx responses
                                throw new TransactionExecutionException();
                            });

                            capturedResponse.Seek(0, SeekOrigin.Begin);

                            context.HttpContext.Response.StatusCode = result.Code;
                            await responseStream.WriteAsync(capturedResponse.GetBuffer(), 0, (int)capturedResponse.Length);

                            context.HttpContext.Response.Body = responseStream;
                        }
                    }
                    catch (TransactionExecutionException e)
                    {
                        //Ignore. The actual exception is going to be handled by the ASP.NET pipeline
                    }
                }
            }
        }
    }

    public class TransactionExecutionException : Exception
    {
    }
}