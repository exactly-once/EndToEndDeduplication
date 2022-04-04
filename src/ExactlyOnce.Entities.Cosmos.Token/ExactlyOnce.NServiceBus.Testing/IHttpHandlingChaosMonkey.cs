namespace ExactlyOnce.NServiceBus.Testing
{
    public interface IHttpHandlingChaosMonkey : IChaosMonkey
    {
        void CreateRequest(string id);
        void EnsureRequestDeleted(string id);
        void CheckExistsRequest(string id);
        void GetRequestBody(string id);
        void GetResponseId(string id);
        void AssociateResponse(string id);

        void StoreResponse(string id);
        void GetResponse(string id);

        //Final
        void EnsureResponseDeleted(string id);
    }
}