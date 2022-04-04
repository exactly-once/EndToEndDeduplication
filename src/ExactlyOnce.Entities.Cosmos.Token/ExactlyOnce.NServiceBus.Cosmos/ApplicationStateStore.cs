using ExactlyOnce.Core;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    public class ApplicationStateStore : IApplicationStateStore<string>
    {
        readonly Container applicationStateContainer;
        readonly JsonSerializer serializer;

        public ApplicationStateStore(Container applicationStateContainer, string partitionKeyProperty)
        {
            this.applicationStateContainer = applicationStateContainer;
            serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new TransactionRecordNamingStrategy(partitionKeyProperty)
                },
                TypeNameHandling = TypeNameHandling.Auto
            });
        }

        public ITransactionRecordContainer<string> Create(string partitionKey)
        {
            return new TransactionRecordContainer(applicationStateContainer, partitionKey, serializer);
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