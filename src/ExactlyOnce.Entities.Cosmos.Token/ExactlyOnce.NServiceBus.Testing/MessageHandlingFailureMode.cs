namespace ExactlyOnce.NServiceBus.Testing
{
    public enum MessageHandlingFailureMode
    {
        LoadTransactionState,
        AddSideEffect,
        BeginStateTransition,
        CommitStateTransition,
        ClearTransactionState,

        TryGetMessage,
        DeleteMessage,
        CheckExistsMessage,
        CreateMessage,

        //Final
        EnsureMessageDeleted,
    }
}