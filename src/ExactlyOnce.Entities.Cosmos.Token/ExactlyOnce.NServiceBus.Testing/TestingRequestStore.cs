using System;
using System.IO;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.MachineInterface;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingRequestStore : IMachineWebInterfaceRequestStore
    {
        IMachineWebInterfaceRequestStore impl;
        IHttpHandlingChaosMonkey chaosMonkey;

        public TestingRequestStore(IMachineWebInterfaceRequestStore impl, IHttpHandlingChaosMonkey chaosMonkey)
        {
            this.impl = impl;
            this.chaosMonkey = chaosMonkey;
        }

        public Task Create(string requestId, Stream requestContent)
        {
            chaosMonkey.CreateRequest(requestId);
            return impl.Create(requestId, requestContent);
        }

        public Task EnsureDeleted(string requestId)
        {
            chaosMonkey.EnsureRequestDeleted(requestId);
            return impl.EnsureDeleted(requestId);
        }

        public Task<bool> CheckExists(string requestId)
        {
            chaosMonkey.CheckExistsRequest(requestId);
            return impl.CheckExists(requestId);
        }

        public Task<Stream> GetBody(string requestId)
        {
            chaosMonkey.GetRequestBody(requestId);
            return impl.GetBody(requestId);
        }

        public Task<string> GetResponseId(string requestId)
        {
            chaosMonkey.GetResponseId(requestId);
            return impl.GetResponseId(requestId);
        }

        public Task AssociateResponse(string requestId, string responseId)
        {
            chaosMonkey.AssociateResponse(requestId);
            return impl.AssociateResponse(requestId, responseId);
        }
    }
}