namespace ExactlyOnce.NServiceBus.Testing
{
    public interface IMessageHandlingChaosMonkey : IChaosMonkey
    {
        void TryGetMessage(string id);
        void DeleteMessage(string id);
        void CheckExistsMessage(string id);

        //Final
        void EnsureMessageDeleted(string id);
    }
}