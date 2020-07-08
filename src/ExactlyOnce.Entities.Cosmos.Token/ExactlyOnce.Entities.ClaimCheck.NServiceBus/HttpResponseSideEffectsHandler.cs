using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class HttpResponseSideEffectsHandler : SideEffectsHandler<HttpResponseRecord>
    {
        readonly IMachineWebInterfaceResponseStore responseStore;
        readonly IMachineWebInterfaceRequestStore requestStore;

        public HttpResponseSideEffectsHandler(
            IMachineWebInterfaceRequestStore requestStore, 
            IMachineWebInterfaceResponseStore responseStore)
        {
            this.requestStore = requestStore;
            this.responseStore = responseStore;
        }

        protected override async Task Publish(string messageId, Guid attemptId, 
            IEnumerable<HttpResponseRecord> committedSideEffects, 
            IEnumerable<HttpResponseRecord> abortedSideEffects)
        {
            foreach (var record in abortedSideEffects)
            {
                await responseStore.EnsureDeleted(record.Id).ConfigureAwait(false);
            }

            var committedResponse = committedSideEffects.Single();
            await requestStore.AssociateResponse(messageId, committedResponse.Id).ConfigureAwait(false);
        }
    }
}