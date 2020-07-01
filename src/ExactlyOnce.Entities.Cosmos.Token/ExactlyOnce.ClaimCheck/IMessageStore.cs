using System;
using System.Threading.Tasks;

namespace ExactlyOnce.ClaimCheck
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