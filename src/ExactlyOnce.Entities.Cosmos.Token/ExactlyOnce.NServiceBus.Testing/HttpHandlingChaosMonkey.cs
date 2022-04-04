namespace ExactlyOnce.NServiceBus.Testing
{
    public class HttpHandlingChaosMonkey : IHttpHandlingChaosMonkey
    {
        ChaosMonkey chaosMonkey;

        public HttpHandlingChaosMonkey(ChaosMonkey chaosMonkey)
        {
            this.chaosMonkey = chaosMonkey;
        }

        public void LoadTransactionState(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.LoadTransactionState);
        }

        public void AddSideEffect(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.AddSideEffect);
        }

        public void BeginStateTransition(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.BeginStateTransition);
        }

        public void CommitStateTransition(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.CommitStateTransition);
        }

        public void ClearTransactionState(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.ClearTransactionState);
        }

        public void CreateMessage(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.CreateMessage);
        }

        public void CreateRequest(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.CreateRequest);
        }

        public void EnsureRequestDeleted(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.EnsureRequestDeleted);
        }

        public void CheckExistsRequest(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.CheckExistsRequest);
        }

        public void GetRequestBody(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.GetRequestBody);
        }

        public void GetResponseId(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.GetResponseId);
        }

        public void AssociateResponse(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.AssociateResponse);
        }

        public void StoreResponse(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.StoreResponse);
        }

        public void GetResponse(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.GetResponse);
        }

        public void EnsureResponseDeleted(string id)
        {
            chaosMonkey.InvokeChaos(id, HttpHandlingFailureMode.EnsureResponseDeleted);
        }
    }
}