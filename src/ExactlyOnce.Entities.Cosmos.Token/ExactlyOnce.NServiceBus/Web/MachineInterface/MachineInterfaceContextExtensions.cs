using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ExactlyOnce.Core;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Messaging.MachineWebInterface;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public static class MachineInterfaceContextExtensions
    {
        public static Task InvokeRest(this IMessageHandlerContext context, string urlTemplate, Stream requestBody,
            object followUpMessage)
        {
            return InvokeRest(context, urlTemplate, new StreamContent(requestBody), followUpMessage);
        }

        public static async Task InvokeRest(this IMessageHandlerContext context, string urlTemplate, HttpContent requestBody,
            object followUpMessage)
        {
            var client = context.Extensions.Get<HttpClient>();

            var uniqueId = Guid.NewGuid().ToString();
            var url = urlTemplate.Replace("{uniqueId}", uniqueId);

            var transactionContext = context.Extensions.Get<ITransactionContext>();
            await transactionContext.AddSideEffect(new HttpRequestRecord
            {
                AttemptId = transactionContext.AttemptId,
                Id = uniqueId,
                Url = url
            }).ConfigureAwait(false);

            var putResponse = await client.PutAsync(url, requestBody).ConfigureAwait(false);
            if (!putResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Error while issuing PUT against URL {url}: {putResponse.StatusCode} {putResponse.ReasonPhrase}");
            }

            var sendOptions = new SendOptions();
            sendOptions.SetHeader("ExactlyOnce.HttpRequestUrl", url);
            await context.Send(followUpMessage, sendOptions).ConfigureAwait(false);
        }

        public static MachineInterfaceResponse GetResponse(this IMessageHandlerContext context)
        {
            if (!context.Extensions.TryGet(out MachineInterfaceResponse response))
            {
                throw new Exception("Cannot access response. The message being processed is not a machine interface follow-up message.");
            }

            return response;
        }
    }
}