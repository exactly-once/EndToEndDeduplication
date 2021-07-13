using System.IO;

// ReSharper disable once CheckNamespace
namespace ExactlyOnce.NServiceBus
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