using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class TransactionRecordContainer
    {
        JsonSerializer serializer;
        static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        const string TransactionEntityId = "_transaction";
        Container container;
        PartitionKey partitionKey;
        string partitionId;
        TransactionRecord value;
        string etag;

        public TransactionRecordContainer(Container container, string partitionId)
        {
            this.container = container;
            this.partitionId = partitionId;
            this.partitionKey = new PartitionKey(partitionId);
            var contractResolver = new DefaultContractResolver();
            contractResolver.NamingStrategy = new TransactionRecordNamingStrategy("AccountNumber");
            serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                TypeNameHandling = TypeNameHandling.Auto
            });
        }


        public string MessageId => value.MessageId;
        public Guid AttemptId
        {
            get
            {
                if (!value.AttemptId.HasValue)
                {
                    throw new Exception("No transaction in progress");
                }
                return value.AttemptId.Value;
            }
        }

        public List<OutgoingMessageRecord> MessageRecords
        {
            get
            {
                if (MessageRecords == null)
                {
                    throw new Exception("No transaction in progress");
                }

                return MessageRecords;
            }
        }

        public async Task Load()
        {
            var response = await container.ReadItemStreamAsync(TransactionEntityId, partitionKey);
            if (response.IsSuccessStatusCode)
            {
                value = FromStream(response.Content);
                etag = value.Etag;
            }
            else
            {
                response = await container.CreateItemStreamAsync(ToStream(new TransactionRecord
                {
                    Id = TransactionEntityId,
                    PartitionId = partitionId
                }), partitionKey);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to create transaction record for partition {partitionKey}");
                }
                value = FromStream(response.Content);
                etag = value.Etag;
            }
        }

        public IReadOnlyCollection<SideEffectRecord> CommittedSideEffects =>
            value.SideEffects
                .Where(x => x.AttemptId == value.AttemptId && x.IncomingId == value.MessageId)
                .ToArray();

        public IReadOnlyCollection<SideEffectRecord> AbortedSideEffects =>
            value.SideEffects
                .Where(x => x.AttemptId != value.AttemptId && x.IncomingId == value.MessageId)
                .ToArray();

        public Task ClearTransactionState()
        {
            value.SideEffects.RemoveAll(x => x.IncomingId == value.MessageId);
            value.MessageId = null;
            value.AttemptId = null;
            return Update();
        }

        public Task AddSideEffect(SideEffectRecord sideEffectRecord)
        {
            if (value.MessageId != null)
            {
                throw new Exception("Cannot add messages if transaction has already been committed.");
            }
            value.SideEffects.Add(sideEffectRecord);
            return Update();
        }

        public Task AddSideEffects(List<SideEffectRecord> sideEffectRecords)
        {
            if (value.MessageId != null)
            {
                throw new Exception("Cannot add messages if transaction has already been committed.");
            }
            value.SideEffects.AddRange(sideEffectRecords);
            return Update();
        }

        public Task CommitTransactionState(string messageId, Guid attemptId, TransactionalBatch batch)
        {
            if (value.MessageId != null)
            {
                throw new Exception("Cannot commit transaction state if another transaction is in progress.");
            }

            value.AttemptId = attemptId;
            value.MessageId = messageId;
            return CreateOrUpdateInBatch(batch);
        }

        async Task Update()
        {
            var options = new ItemRequestOptions
            {
                IfMatchEtag = etag
            };
            var response = await container.ReplaceItemStreamAsync(ToStream(value), TransactionEntityId, partitionKey, options);
            value = FromStream(response.Content);
            etag = value.Etag;
        }

        async Task CreateOrUpdateInBatch(TransactionalBatch batch)
        {
            if (etag == null)
            {
                batch.CreateItemStream(ToStream(value));
            }
            else
            {
                var options = new TransactionalBatchItemRequestOptions
                {
                    IfMatchEtag = etag
                };
                batch.ReplaceItemStream(TransactionEntityId, ToStream(value), options);
            }

            var response = await batch.ExecuteAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ToString());
            }
            etag = response.Last().ETag;
        }


        TransactionRecord FromStream(Stream stream)
        {
            using (stream)
            {
                using (var sr = new StreamReader(stream))
                {
                    using (var jsonTextReader = new JsonTextReader(sr))
                    {
                        return serializer.Deserialize<TransactionRecord>(jsonTextReader);
                    }
                }
            }
        }

        Stream ToStream(TransactionRecord input)
        {
            var streamPayload = new MemoryStream();
            using (var streamWriter = new StreamWriter(streamPayload, DefaultEncoding, 1024, true))
            {
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Formatting.None;
                    serializer.Serialize(writer, input);
                    writer.Flush();
                    streamWriter.Flush();
                }
            }

            streamPayload.Position = 0;
            return streamPayload;
        }

        class TransactionRecordNamingStrategy : NamingStrategy
        {
            readonly string partitionIdName;

            public TransactionRecordNamingStrategy(string partitionIdName)
            {
                this.partitionIdName = partitionIdName;
            }

            protected override string ResolvePropertyName(string name)
            {
                if (name == "PartitionId")
                {
                    return partitionIdName;
                }
                return name;
            }
        }
    }
}