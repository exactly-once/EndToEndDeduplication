namespace PaymentProvider.Frontend
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using ExactlyOnce.NServiceBus;
    using Newtonsoft.Json;

    public static class MachineInterfaceConnectorExtensions
    {
        static readonly JsonSerializer serializer = new JsonSerializer();

        public static Task<StoredResponse> ExecuteTransaction<TPayload, T>(this IMachineInterfaceConnector<T> connector, string requestId,
            Func<TPayload, T> getPartitionKey,
            Func<IMachineInterfaceConnectorMessageSession<TPayload>, Task<StoredResponse>> transaction)
        {
            return connector.ExecuteTransaction(requestId, async stream =>
            {
                using (var streamReader = new StreamReader(stream))
                using (var reader = new JsonTextReader(streamReader))
                {
                    var request = serializer.Deserialize<TPayload>(reader);

                    var partitionKey = getPartitionKey(request);
                    return (request, partitionKey);
                }
            }, transaction);
        }
    }
}