using System.IO;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus.Web.MachineInterface;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingResponseStore : IMachineWebInterfaceResponseStore
    {
        IMachineWebInterfaceResponseStore impl;
        readonly IHttpHandlingChaosMonkey chaosMonkey;

        public TestingResponseStore(IMachineWebInterfaceResponseStore impl, IHttpHandlingChaosMonkey chaosMonkey)
        {
            this.impl = impl;
            this.chaosMonkey = chaosMonkey;
        }

        public Task Store(string responseId, int responseStatus, Stream responseBody)
        {
            //TODO No request Id
            return impl.Store(responseId, responseStatus, responseBody);
        }

        public Task<StoredResponse> Get(string responseId)
        {
            //TODO No request Id
            return impl.Get(responseId);
        }

        public Task EnsureDeleted(string responseId)
        {
            //TODO No request Id
            return impl.EnsureDeleted(responseId);
        }
    }
}