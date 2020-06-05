using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface ISideEffectsHandler
    {
        Task Prepare(List<OutboxRecord> records);
        Task Commit(List<OutboxRecord> records);
    }
}