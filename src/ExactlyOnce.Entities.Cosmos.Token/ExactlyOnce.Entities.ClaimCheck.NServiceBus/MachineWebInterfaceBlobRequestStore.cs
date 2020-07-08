using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class MachineWebInterfaceBlobRequestStore : IMachineWebInterfaceRequestStore
    {
        readonly BlobContainerClient containerClient;
        readonly string prefix;

        public MachineWebInterfaceBlobRequestStore(BlobContainerClient containerClient, string prefix)
        {
            this.containerClient = containerClient;
            this.prefix = prefix;
        }

        public Task Create(string requestId, Stream requestContent)
        {
            return containerClient.GetBlobClient(BlobName(requestId)).UploadAsync(requestContent);
        }

        public Task EnsureDeleted(string requestId)
        {
            return containerClient.GetBlobClient(BlobName(requestId)).DeleteIfExistsAsync();
        }

        public async Task<Stream> GetBody(string requestId)
        {
            try
            {
                var response = await containerClient.GetBlobClient(BlobName(requestId)).DownloadAsync().ConfigureAwait(false);
                return response.Value.Content;
            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    return null;
                }
                throw new Exception($"Error while downloading request content for request {requestId}");
            }
        }

        public async Task<string> GetResponseId(string requestId)
        {
            var result = await containerClient.GetBlobClient(BlobName(requestId)).GetPropertiesAsync()
                .ConfigureAwait(false);
            return result.Value.Metadata["ResponseId"];
        }

        public Task AssociateResponse(string requestId, string responseId)
        {
            var metadata = new Dictionary<string, string>
            {
                ["ResponseId"] = responseId
            };
            return containerClient.GetBlobClient(BlobName(requestId)).SetMetadataAsync(metadata);
        }

        string BlobName(string requestId)
        {
            return prefix + requestId;
        }
    }
}