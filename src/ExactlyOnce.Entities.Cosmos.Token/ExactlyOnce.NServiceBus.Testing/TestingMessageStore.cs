using System.Threading.Tasks;

namespace ExactlyOnce.NServiceBus.Testing
{
    public class TestingMessageStore : IMessageStore
    {
        IMessageStore impl;
        IChaosMonkey chaosMonkey;
        IMessageHandlingChaosMonkey messageHandlingChaosMonkey;

        public TestingMessageStore(IMessageStore impl, IChaosMonkey chaosMonkey)
        {
            this.chaosMonkey = chaosMonkey;
            this.impl = impl;
            messageHandlingChaosMonkey = chaosMonkey as IMessageHandlingChaosMonkey;
        }

        public Task<byte[]> TryGet(string messageId)
        {
            messageHandlingChaosMonkey?.TryGetMessage(messageId);
            return impl.TryGet(messageId);
        }

        public Task Delete(string messageId)
        {
            messageHandlingChaosMonkey?.DeleteMessage(messageId);
            return impl.Delete(messageId);
        }

        public Task<bool> CheckExists(string messageId)
        {
            messageHandlingChaosMonkey?.CheckExistsMessage(messageId);
            return impl.CheckExists(messageId);
        }

        public Task Create(string sourceId, Message[] messages)
        {
            chaosMonkey.CreateMessage(sourceId);
            return impl.Create(sourceId, messages);
        }

        public Task EnsureDeleted(string[] messageIds)
        {
            //TODO
            return impl.EnsureDeleted(messageIds);
        }
    }
}
