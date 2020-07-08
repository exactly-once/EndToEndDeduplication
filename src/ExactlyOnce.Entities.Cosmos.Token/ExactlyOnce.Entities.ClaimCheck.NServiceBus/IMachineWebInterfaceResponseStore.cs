using System.IO;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public interface IMachineWebInterfaceResponseStore
    {
        Task Store(string responseId, int responseStatus, Stream responseBody);
        Task<StoredResponse> Get(string responseId);
        Task EnsureDeleted(string responseId);
    }
}