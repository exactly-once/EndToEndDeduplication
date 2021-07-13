using System.Threading.Tasks;

namespace ExactlyOnce.NServiceBus
{
    public interface IMessageStore
    {
        Task<byte[]> TryGet(string messageId);

        Task Delete(string messageId);
        Task<bool> CheckExists(string messageId);
        
        Task Create(Message[] messages);
        Task EnsureDeleted(string[] messageIds);
    }
}