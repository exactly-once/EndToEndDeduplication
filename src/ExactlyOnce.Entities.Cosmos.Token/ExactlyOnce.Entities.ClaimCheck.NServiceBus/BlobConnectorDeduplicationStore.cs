using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Newtonsoft.Json;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class BlobConnectorDeduplicationStore : IConnectorDeduplicationStore
    {
        readonly BlobContainerClient containerClient;
        static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
        readonly JsonSerializer serializer = new JsonSerializer();

        public BlobConnectorDeduplicationStore(BlobContainerClient containerClient)
        {
            this.containerClient = containerClient;
        }

        public async Task<ResponseMessage> HasBeenProcessed(string requestId)
        {
            try
            {
                var response = await containerClient.GetBlobClient(requestId).DownloadAsync().ConfigureAwait(false);
                var result = serializer.Deserialize<ResponseMessage>(new JsonTextReader(new StreamReader(response.Value.Content)));
                return result;
            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    return null;
                }

                throw;
            }
        }

        public Task MarkProcessed(string requestId, ResponseMessage message)
        {
            using (var memStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(memStream, DefaultEncoding, 1024, true))
                {
                    serializer.Serialize(writer, message);
                }
                memStream.Seek(0, SeekOrigin.Begin);
                return containerClient.GetBlobClient(requestId).UploadAsync(memStream);
            }
        }
    }
}