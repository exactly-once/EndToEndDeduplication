using System;
using System.Net.Http;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace ExactlyOnce.NServiceBus.Messaging.MachineWebInterface
{
    class MachineInterfaceFollowUpMessageHandlingBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        readonly HttpClient httpClient;

        public MachineInterfaceFollowUpMessageHandlingBehavior(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            if (!context.Message.Headers.TryGetValue("ExactlyOnce.HttpRequestUrl", out var requestUrl))
            {
                await next().ConfigureAwait(false);
                return;
            }

            var postResponse = await httpClient.PostAsync(requestUrl, new StringContent(""))
                .ConfigureAwait(false);

            var statusCodeInt = (int) postResponse.StatusCode;
            if (statusCodeInt > 499)
            {
                throw new Exception($"Server error {statusCodeInt} while issuing POST request against URL {requestUrl}. Retrying.");
            }

            var bodyStream = await postResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var responseObject = new MachineInterfaceResponse(postResponse.StatusCode, bodyStream);
            context.Extensions.Set(responseObject);

            await next().ConfigureAwait(false);

            var deleteResponse = await httpClient.DeleteAsync(requestUrl).ConfigureAwait(false);
            if (!deleteResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Error while issuing DELETE request against URL {requestUrl}. Retrying.");
            }
        }
    }
}