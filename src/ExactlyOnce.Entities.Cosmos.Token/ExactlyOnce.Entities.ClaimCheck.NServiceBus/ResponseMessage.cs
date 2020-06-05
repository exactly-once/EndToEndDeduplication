using System.IO;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    /// <summary>
    /// Recorded HTTP response message
    /// </summary>
    public class ResponseMessage
    {
        public int StatusCode { get; set; }
    }
}