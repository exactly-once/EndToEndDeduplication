using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Messaging.MachineWebInterface
{
    class MachineInterfaceInvocationSideEffectsHandler : SideEffectsHandler<HttpRequestRecord>
    {
        HttpClient httpClient;

        public MachineInterfaceInvocationSideEffectsHandler(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        protected override async Task Publish(string messageId, Guid attemptId, IEnumerable<HttpRequestRecord> committedSideEffects, IEnumerable<HttpRequestRecord> abortedSideEffects)
        {
            var abortCalls = abortedSideEffects.Select(x => httpClient.DeleteAsync(x.Url));
            await Task.WhenAll(abortCalls).ConfigureAwait(false);

            //Do nothing with the committed calls
            //They will be handled when the follow-up message is received
        }
    }
}