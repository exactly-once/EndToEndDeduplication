using System.Collections.Generic;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class OutboxRecord
    {
        public OutboxRecord(string type, Dictionary<string, string> properties, byte[] binaryPayload,
            Dictionary<string, string> metadata)
        {
            Metadata = metadata;
            Type = type;
            BinaryPayload = binaryPayload;
            Properties = properties;
        }

        public string Type { get; }
        public Dictionary<string, string> Metadata { get; }
        public Dictionary<string, string> Properties { get; }
        public byte[] BinaryPayload { get; }
    }
}