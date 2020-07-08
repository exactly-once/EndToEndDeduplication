using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class MachineWebInterfaceBlobResponseStore : IMachineWebInterfaceResponseStore
    {
        readonly BlobContainerClient containerClient;
        readonly string prefix;

        public MachineWebInterfaceBlobResponseStore(BlobContainerClient containerClient, string prefix)
        {
            this.containerClient = containerClient;
            this.prefix = prefix;
        }

        public Task Store(string responseId, int responseStatus, Stream responseBody)
        {
            var metadata = new Dictionary<string, string>
            {
                ["StatusCode"] = responseStatus.ToString()
            };
            if (responseBody == null)
            {
                metadata["EmptyBody"] = "true";
                responseBody = new MemoryStream(new byte[0]); //empty stream
            }
            return containerClient.GetBlobClient(BlobName(responseId)).UploadAsync(responseBody, null, metadata);
        }

        public async Task<StoredResponse> Get(string responseId)
        {
            try
            {
                var response = await containerClient.GetBlobClient(BlobName(responseId)).DownloadAsync().ConfigureAwait(false);
                var status = int.Parse(response.Value.Details.Metadata["StatusCode"]);
                var empty = response.Value.Details.Metadata.ContainsKey("EmptyBody");
                var body = empty ? null : response.Value.Content;
                return new StoredResponse(status, body);
            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    return null;
                }
                throw new Exception($"Error while downloading stored response {responseId}");
            }
        }

        public Task EnsureDeleted(string responseId)
        {
            return containerClient.GetBlobClient(BlobName(responseId)).DeleteIfExistsAsync();
        }

        string BlobName(string responseId)
        {
            return prefix + responseId;
        }
    }
}