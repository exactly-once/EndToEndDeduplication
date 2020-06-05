using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class BlobOutboxStore : IOutboxStore
    {
        readonly BlobContainerClient containerClient;

        public BlobOutboxStore(BlobContainerClient containerClient)
        {
            this.containerClient = containerClient;
        }

        public async Task<OutboxState> Get(Guid attemptId)
        {
            try
            {
                var response = await containerClient.GetBlobClient(attemptId.ToString("D")).DownloadAsync().ConfigureAwait(false);
                return Deserialize(response.Value.Content);
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

        public async Task Store(Guid attemptId, OutboxState outboxState)
        {
            using (var stream = Serialize(outboxState))
            {
                await containerClient.GetBlobClient(attemptId.ToString("D")).UploadAsync(stream).ConfigureAwait(false);
            }
        }

        public Task Remove(Guid attemptId)
        {
            return containerClient.GetBlobClient(attemptId.ToString("D")).DeleteAsync();
        }

        static OutboxState Deserialize(Stream data)
        {
            var reader = new BinaryReader(data);
            var version = reader.ReadByte();
            if (version != 1)
            {
                throw new Exception($"Incompatible outbox data version: {version}");
            }

            var partitionId = reader.ReadString();
            var transactionId = reader.ReadString();
            var result = new List<OutboxRecord>();
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var type = reader.ReadString();
                var properties = DeserializeDictionary(reader);
                var options = DeserializeDictionary(reader);
                var bodyLength = reader.ReadInt32();
                var binaryPayload = reader.ReadBytes(bodyLength);

                var message = new OutboxRecord(type, properties, binaryPayload, options);
                result.Add(message);
            }

            return new OutboxState(partitionId, transactionId, result);
        }

        static Dictionary<string, string> DeserializeDictionary(BinaryReader reader)
        {
            var itemCount = reader.ReadInt32();
            var result = new Dictionary<string, string>();
            for (var header = 0; header < itemCount; header++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                result[key] = value;
            }

            return result;
        }

        static Stream Serialize(OutboxState outboxState)
        {
            var memory = new MemoryStream();
            using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
            {
                writer.Write((byte)1);
                writer.Write(outboxState.PartitionId);
                writer.Write(outboxState.TransactionId);
                writer.Write(outboxState.Records.Count);
                foreach (var outboxMessage in outboxState.Records)
                {
                    writer.Write(outboxMessage.Type);
                    SerializeDictionary(writer, outboxMessage.Properties);
                    SerializeDictionary(writer, outboxMessage.Metadata);
                    writer.Write(outboxMessage.BinaryPayload.Length);
                    writer.Write(outboxMessage.BinaryPayload);
                }
            }

            memory.Seek(0, SeekOrigin.Begin);
            return memory;
        }

        static void SerializeDictionary(BinaryWriter writer, Dictionary<string, string> value)
        {
            writer.Write(value.Count);
            foreach (var pair in value)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

    }
}