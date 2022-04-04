namespace ExactlyOnce.NServiceBus.Testing
{
    public interface IChaosMonkey
    {
        void LoadTransactionState(string id);
        void AddSideEffect(string id);
        void BeginStateTransition(string id);
        void CommitStateTransition(string id);
        void ClearTransactionState(string id);

        
        void CreateMessage(string id);
    }
}