using System.IO;
using System.Net;

namespace ExactlyOnce.NServiceBus.Messaging.MachineWebInterface
{
    public class MachineInterfaceResponse
    {
        public HttpStatusCode Status;
        public Stream Body;

        public MachineInterfaceResponse(HttpStatusCode status, Stream body)
        {
            Status = status;
            Body = body;
        }
    }
}