using System;
using System.IO;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.MachineInterface;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingResponseStore : IMachineWebInterfaceResponseStore
    {
        IMachineWebInterfaceResponseStore impl;
        public Task Store(string responseId, int responseStatus, Stream responseBody)
        {
            return impl.Store(responseId, responseStatus, responseBody);
        }

        public Task<StoredResponse> Get(string responseId)
        {
            return impl.Get(responseId);
        }

        public Task EnsureDeleted(string responseId)
        {
            return impl.EnsureDeleted(responseId);
        }
    }
}