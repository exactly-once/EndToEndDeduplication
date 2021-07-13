using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using ExactlyOnce.NServiceBus.Web.MachineInterface;

namespace ExactlyOnce.NServiceBus.Blob
{
    public class MachineWebInterfaceRequestStore : IMachineWebInterfaceRequestStore
    {
        readonly BlobContainerClient containerClient;
        readonly string prefix;

        public MachineWebInterfaceRequestStore(BlobContainerClient containerClient, string prefix)
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

        public async Task<bool> CheckExists(string requestId)
        {
            try
            {
                var result = await containerClient.GetBlobClient(BlobName(requestId)).GetPropertiesAsync()
                    .ConfigureAwait(false);

                //If metadata is present we return false to indicate the token for processing that message no longer exists
                return !result.Value.Metadata.TryGetValue("ResponseId", out _); 
            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    return false;
                }
                throw new Exception($"Error while retrieving response ID for request {requestId}", e);
            }
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
                throw new Exception($"Error while downloading request content for request {requestId}", e);
            }
        }

        public async Task<string> GetResponseId(string requestId)
        {
            try
            {
                var result = await containerClient.GetBlobClient(BlobName(requestId)).GetPropertiesAsync()
                .ConfigureAwait(false);

                return result.Value.Metadata.TryGetValue("ResponseId", out var responseId) ? responseId : null;
            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    return null;
                }
                throw new Exception($"Error while retrieving response ID for request {requestId}", e);
            }
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