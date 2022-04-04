using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ExactlyOnce.NServiceBus.Blob
{
    public class MessageStore : IMessageStore
    {
        readonly BlobContainerClient containerClient;

        public MessageStore(BlobContainerClient containerClient)
        {
            this.containerClient = containerClient;
        }

        public Task Delete(string messageId)
        {
            return containerClient.GetBlobClient(messageId).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }

        public Task EnsureDeleted(string[] messageIds)
        {
            var deleteTasks = messageIds.Select(x =>
                containerClient.GetBlobClient(x).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots));

            return Task.WhenAll(deleteTasks);
        }

        public async Task<byte[]> TryGet(string id)
        {
            try
            {
                var response = await containerClient.GetBlobClient(id).DownloadContentAsync().ConfigureAwait(false);
                return response.Value.Content.ToArray();
            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    return null;
                }
                throw new Exception($"Error while loading the message body from token {id}", e);
            }
        }

        public async Task<bool> CheckExists(string id)
        {
            var response = await containerClient.GetBlobClient(id).ExistsAsync().ConfigureAwait(false);
            return response.Value;
        }

        public Task Create(string sourceId, Message[] messages)
        {
            var tasks = messages.Select(async m =>
            {
                using (var stream = new MemoryStream(m.Body))
                {
                    await containerClient.GetBlobClient(m.MessageId).UploadAsync(stream).ConfigureAwait(false);
                }
            });

            return Task.WhenAll(tasks);
        }
    }
}
