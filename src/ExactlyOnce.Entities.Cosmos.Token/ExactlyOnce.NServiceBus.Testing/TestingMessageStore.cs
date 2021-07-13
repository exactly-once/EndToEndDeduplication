using System;
using System.Threading.Tasks;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingMessageStore : IMessageStore
    {
        IMessageStore impl;
        public Task<byte[]> TryGet(string messageId)
        {
            return impl.TryGet(messageId);
        }

        public Task Delete(string messageId)
        {
            return impl.Delete(messageId);
        }

        public Task<bool> CheckExists(string messageId)
        {
            return impl.CheckExists(messageId);
        }

        public Task Create(Message[] messages)
        {
            return impl.Create(messages);
        }

        public Task EnsureDeleted(string[] messageIds)
        {
            return impl.EnsureDeleted(messageIds);
        }
    }
}
