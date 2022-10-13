using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.Core;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.NServiceBus.Web.MachineInterface
{
    class MachineInterfaceConnector<T> : IMachineInterfaceConnector<T>
    {
        readonly IApplicationStateStore<T> applicationStateStore;
        readonly IMessageSession rootMessageSession;
        readonly ExactlyOnceProcessor<object> processor;
        readonly IMessageStore messageStore;
        readonly IMachineWebInterfaceRequestStore requestStore;
        readonly IMachineWebInterfaceResponseStore responseStore;

        public MachineInterfaceConnector(IApplicationStateStore<T> applicationStateStore, 
            IEnumerable<ISideEffectsHandler> sideEffectsHandlers, 
            IMessageSession rootMessageSession, 
            IDispatchMessages dispatcher, 
            IMessageStore messageStore, 
            IMachineWebInterfaceRequestStore requestStore, 
            IMachineWebInterfaceResponseStore responseStore)
        {
            this.applicationStateStore = applicationStateStore;
            this.rootMessageSession = rootMessageSession;
            this.messageStore = messageStore;
            this.requestStore = requestStore;
            this.responseStore = responseStore;

            var allHandlers = sideEffectsHandlers.Concat(new ISideEffectsHandler[]
            {
                new WebMessagingWithClaimCheckSideEffectsHandler(messageStore, dispatcher), 
                new HttpResponseSideEffectsHandler(requestStore, responseStore), 
            });
            processor = new ExactlyOnceProcessor<object>(allHandlers.ToArray(), new NServiceBusDebugLogger());
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
            if (responseId != null)
            {
                await responseStore.EnsureDeleted(responseId);
            }
            await requestStore.EnsureDeleted(requestId);
        }

        public async Task<StoredResponse> ExecuteTransaction(string requestId, T partitionKey, Func<IMachineInterfaceConnectorMessageSession, Task<StoredResponse>> transaction)
        {
            var requestBody = await requestStore.GetBody(requestId).ConfigureAwait(false);
            if (requestBody == null)
            {
                //The client did not follow the protocol and issued a POST after a DELETE
                return new StoredResponse(400, null);
            }

            var transactionRecordContainer = applicationStateStore.Create(partitionKey);

            var processingResult = await processor.Process(requestId, transactionRecordContainer, null, async (ctx, transactionContext) =>
            {
                var requestTokenExists = await requestStore.CheckExists(requestId).ConfigureAwait(false);
                if (!requestTokenExists)
                {
                    return ProcessingResult<StoredResponse>.Duplicate;
                }

                var session = new MachineInterfaceConnectorMessageSession(requestId, requestBody, transactionContext, rootMessageSession, messageStore);
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

        //POST
        public async Task<StoredResponse> ExecuteTransaction<TPayload>(string requestId, 
            Func<Stream, Task<(TPayload, T)>> getPayloadAndPartitionKey,
            Func<IMachineInterfaceConnectorMessageSession<TPayload>, Task<StoredResponse>> transaction)
        {
            var requestBody = await requestStore.GetBody(requestId).ConfigureAwait(false);
            if (requestBody == null)
            {
                //The client did not follow the protocol and issued a POST after a DELETE
                return new StoredResponse(400, null);
            }

            var payloadAndPartitionKey = await getPayloadAndPartitionKey(requestBody).ConfigureAwait(false);

            var transactionRecordContainer = applicationStateStore.Create(payloadAndPartitionKey.Item2);

            var processingResult = await processor.Process(requestId, transactionRecordContainer, null, async (ctx, transactionContext) =>
            {
                var requestTokenExists = await requestStore.CheckExists(requestId).ConfigureAwait(false);
                if (!requestTokenExists)
                {
                    return ProcessingResult<StoredResponse>.Duplicate;
                }

                var session = new MachineInterfaceConnectorMessageSession<TPayload>(requestId, payloadAndPartitionKey.Item1, transactionContext, rootMessageSession, messageStore);
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