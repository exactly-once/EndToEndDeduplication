using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Messaging.MachineWebInterface
{
    public class HttpRequestRecord : SideEffectRecord
    {
        public string Id { get; set; }
        public string Url { get; set; }
    }
}