using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Web.MachineInterface
{
    public class HttpResponseRecord : SideEffectRecord
    {
        public string Id { get; set; }
    }
}