using System;
using System.IO;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.MachineInterface;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingRequestStore : IMachineWebInterfaceRequestStore
    {
        IMachineWebInterfaceRequestStore impl;
        public Task Create(string requestId, Stream requestContent)
        {
            return impl.Create(requestId, requestContent);
        }

        public Task EnsureDeleted(string requestId)
        {
            return impl.EnsureDeleted(requestId);
        }

        public Task<bool> CheckExists(string requestId)
        {
            return impl.CheckExists(requestId);
        }

        public Task<Stream> GetBody(string requestId)
        {
            return impl.GetBody(requestId);
        }

        public Task<string> GetResponseId(string requestId)
        {
            return impl.GetResponseId(requestId);
        }

        public Task AssociateResponse(string requestId, string responseId)
        {
            return impl.AssociateResponse(requestId, responseId);
        }
    }
}