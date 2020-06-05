using System;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface IOutboxStore
    {
        Task<OutboxState> Get(Guid attemptId);
        Task Store(Guid attemptId, OutboxState outboxState);
        Task Remove(Guid attemptId);
    }
}