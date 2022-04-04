namespace ExactlyOnce.NServiceBus.Testing
{
    public enum HttpHandlingFailureMode
    {
        LoadTransactionState,
        AddSideEffect,
        BeginStateTransition,
        CommitStateTransition,
        ClearTransactionState,

        CreateMessage,

        CreateRequest,
        EnsureRequestDeleted,
        CheckExistsRequest,
        GetRequestBody,
        GetResponseId,
        AssociateResponse,

        StoreResponse,
        GetResponse,

        //Final
        EnsureResponseDeleted
    }
}