using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExactlyOnce.Core;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    class TransactionRecordContainer : ITransactionRecordContainer<string>
    {
        static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
        const string TransactionEntityId = "_transaction";

        readonly JsonSerializer serializer;
        readonly Container container;
        readonly PartitionKey partitionKey;
        readonly string partitionId;
        TransactionRecord value;
        string etag;
        TransactionBatchContext batchContext;

        public TransactionBatchContext BatchContext => batchContext;

        public TransactionRecordContainer(Container container, string partitionId, JsonSerializer serializer)
        {
            this.container = container;
            this.partitionId = partitionId;
            this.serializer = serializer;
            partitionKey = new PartitionKey(partitionId);
        }

        public string UniqueIdentifier => partitionId;
        public object Unwrap()
        {
            return this;
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
                .Where(x => x.AttemptId == value.AttemptId)
                .ToArray();

        public IReadOnlyCollection<SideEffectRecord> AbortedSideEffects =>
            value.SideEffects
                .Where(x => x.AttemptId != value.AttemptId)
                .ToArray();

        public Task ClearTransactionState()
        {
            value.SideEffects.Clear();
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

        public Task BeginStateTransition()
        {
            if (batchContext != null)
            {
                throw new Exception("Transaction batch already created.");
            }
            if (value.MessageId != null)
            {
                throw new Exception("Cannot begin state transition if another transition is in progress.");
            }
            var batch = container.CreateTransactionalBatch(partitionKey);
            batchContext = new TransactionBatchContext(batch, container, partitionKey);
            return Task.CompletedTask;
        }

        public Task CommitStateTransition(string messageId, Guid attemptId)
        {
            if (value.MessageId != null)
            {
                throw new Exception("Cannot commit transaction state if another transaction is in progress.");
            }

            value.AttemptId = attemptId;
            value.MessageId = messageId;
            return CreateOrUpdateInBatch(batchContext.Batch);
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

    }
}