using System.IO;
using System.Threading.Tasks;

namespace ExactlyOnce.NServiceBus.Web.MachineInterface
{
    public interface IMachineWebInterfaceResponseStore
    {
        Task Store(string responseId, int responseStatus, Stream responseBody);
        Task<StoredResponse> Get(string responseId);
        Task EnsureDeleted(string responseId);
    }
}