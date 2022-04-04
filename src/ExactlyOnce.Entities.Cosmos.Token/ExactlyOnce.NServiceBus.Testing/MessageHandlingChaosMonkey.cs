namespace ExactlyOnce.NServiceBus.Testing
{
    public class MessageHandlingChaosMonkey : IMessageHandlingChaosMonkey
    {
        ChaosMonkey chaosMonkey;

        public MessageHandlingChaosMonkey(ChaosMonkey chaosMonkey)
        {
            this.chaosMonkey = chaosMonkey;
        }

        public void LoadTransactionState(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.LoadTransactionState);
        }

        public void AddSideEffect(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.AddSideEffect);
        }

        public void BeginStateTransition(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.BeginStateTransition);
        }

        public void CommitStateTransition(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.CommitStateTransition);
        }

        public void ClearTransactionState(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.ClearTransactionState);
        }

        public void CreateMessage(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.CreateMessage);
        }

        public void TryGetMessage(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.TryGetMessage);
        }

        public void DeleteMessage(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.DeleteMessage);
        }

        public void CheckExistsMessage(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.CheckExistsMessage);
        }

        public void EnsureMessageDeleted(string id)
        {
            chaosMonkey.InvokeChaos(id, MessageHandlingFailureMode.EnsureMessageDeleted);
        }
    }
}