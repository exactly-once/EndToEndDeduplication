using System.IO;
using System.Net;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
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