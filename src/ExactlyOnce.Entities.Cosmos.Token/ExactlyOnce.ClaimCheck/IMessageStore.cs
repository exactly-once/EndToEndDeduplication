using System.Threading.Tasks;

namespace ExactlyOnce.ClaimCheck
{
    public interface IMessageStore
    {
        Task Delete(string messageId);
        Task<byte[]> TryGet(string id);
        Task<bool> CheckExists(string id);
        Task Create(Message[] messages);
    }
}