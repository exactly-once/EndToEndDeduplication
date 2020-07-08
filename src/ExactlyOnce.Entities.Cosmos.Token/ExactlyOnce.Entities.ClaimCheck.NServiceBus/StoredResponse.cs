using System.IO;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class StoredResponse
    {
        public StoredResponse(int code, Stream body)
        {
            Code = code;
            Body = body;
        }

        public int Code { get; }
        public Stream Body { get; }
    }
}