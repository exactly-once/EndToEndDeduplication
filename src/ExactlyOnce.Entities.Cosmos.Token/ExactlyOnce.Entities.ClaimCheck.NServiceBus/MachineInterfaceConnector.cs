using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class MachineInterfaceConnector : IMachineInterfaceConnector
    {
        readonly IMessageSession rootMessageSession;
        readonly ExactlyOnceProcessor<object, StoredResponse> processor;
        readonly IMessageStore messageStore;
        readonly IMachineWebInterfaceRequestStore requestStore;
        readonly IMachineWebInterfaceResponseStore responseStore;

        public MachineInterfaceConnector(Container applicationStoreContainer, 
            IEnumerable<ISideEffectsHandler> sideEffectsHandlers, 
            IMessageSession rootMessageSession, 
            IDispatchMessages dispatcher, 
            IMessageStore messageStore, 
            IMachineWebInterfaceRequestStore requestStore, 
            IMachineWebInterfaceResponseStore responseStore)
        {
            this.rootMessageSession = rootMessageSession;
            this.messageStore = messageStore;
            this.requestStore = requestStore;
            this.responseStore = responseStore;

            var allHandlers = sideEffectsHandlers.Concat(new ISideEffectsHandler[]
            {
                new WebMessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher), 
                new HttpResponseSideEffectsHandler(requestStore, responseStore), 
            });
            processor = new ExactlyOnceProcessor<object, StoredResponse>(applicationStoreContainer, 
                new SideEffectsHandlerCollection(allHandlers.ToArray()));
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        //PUT
        public Task StoreRequest(string requestId, Stream body)
        {
            return requestStore.Create(requestId, body);
        }

        /// <summary>
        /// Removes the response and the request token.
        /// </summary>
        public async Task DeleteResponse(string requestId)
        {
            var responseId = await requestStore.GetResponseId(requestId).ConfigureAwait(false);
            await responseStore.EnsureDeleted(responseId);
            await requestStore.EnsureDeleted(requestId);
        }

        //POST
        public async Task<StoredResponse> ExecuteTransaction(string requestId, string partitionKey, Func<IMachineInterfaceConnectorMessageSession, Task<StoredResponse>> transaction)
        {
            var processingResult = await processor.Process(requestId, partitionKey, null, async (ctx, transactionBatch, transactionContext) =>
            {
                var requestBody = await requestStore.GetBody(requestId).ConfigureAwait(false);
                if (requestBody == null)
                {
                    return ProcessingResult<StoredResponse>.Duplicate;
                }

                var session = new MachineInterfaceConnectorMessageSession(requestId, requestBody, transactionBatch, transactionContext, rootMessageSession, messageStore);
                var response = await transaction(session).ConfigureAwait(false);

                var responseId = Guid.NewGuid().ToString();
                await transactionContext.AddSideEffect(new HttpResponseRecord
                {
                    AttemptId = transactionContext.AttemptId,
                    IncomingId = requestId,
                    Id = responseId
                });

                await responseStore.Store(responseId, response.Code, response.Body).ConfigureAwait(false);

                return ProcessingResult<StoredResponse>.Successful(response);
            }).ConfigureAwait(false);
            if (!processingResult.IsDuplicate)
            {
                return processingResult.Value;
            }

            var storedResponseId = await requestStore.GetResponseId(requestId).ConfigureAwait(false);
            if (storedResponseId == null)
            {
                //The client did not follow the protocol and issued a POST after a DELETE
                return new StoredResponse(400, null);
            }
            var storedResponse = await responseStore.Get(storedResponseId).ConfigureAwait(false);
            if (storedResponse == null)
            {
                //The client did not follow the protocol and issued a POST after a DELETE
                return new StoredResponse(400, null);
            }
            return storedResponse;

        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

    }
}