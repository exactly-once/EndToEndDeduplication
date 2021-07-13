using System.IO;
using System.Threading.Tasks;

namespace ExactlyOnce.NServiceBus.Web.MachineInterface
{
    public interface IMachineWebInterfaceRequestStore
    {
        Task Create(string requestId, Stream requestContent);
        Task EnsureDeleted(string requestId);
        Task<bool> CheckExists(string requestId);
        Task<Stream> GetBody(string requestId);
        Task<string> GetResponseId(string requestId);
        Task AssociateResponse(string requestId, string responseId);
    }
}