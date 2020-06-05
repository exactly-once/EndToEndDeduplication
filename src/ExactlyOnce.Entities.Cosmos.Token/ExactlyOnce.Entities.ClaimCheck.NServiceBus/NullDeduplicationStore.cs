using System.Threading.Tasks;
using ExactlyOnce.Entities.ClaimCheck.NServiceBus;

namespace NServiceBus
{
    class NullDeduplicationStore : IConnectorDeduplicationStore
    {
        static readonly Task<ResponseMessage> emptyResult = Task.FromResult<ResponseMessage>(null);

        public Task<ResponseMessage> HasBeenProcessed(string requestId)
        {
            return emptyResult;
        }

        public Task MarkProcessed(string requestId, ResponseMessage response)
        {
            return Task.CompletedTask;
        }
    }
}