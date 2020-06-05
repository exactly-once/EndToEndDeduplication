using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class DeduplicationStoreResponseSideEffectsHandler : ISideEffectsHandler
    {
        readonly IConnectorDeduplicationStore deduplicationStore;

        public DeduplicationStoreResponseSideEffectsHandler(IConnectorDeduplicationStore deduplicationStore)
        {
            this.deduplicationStore = deduplicationStore;
        }

        public Task Prepare(List<OutboxRecord> records)
        {
            var responseRecord = records.Single();
            return deduplicationStore.MarkProcessed(responseRecord.Metadata["RequestId"], responseRecord.ToResponseMessage());
        }

        public Task Commit(List<OutboxRecord> records)
        {
            return Task.CompletedTask;
        }
    }
}