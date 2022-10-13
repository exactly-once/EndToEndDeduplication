

// ReSharper disable once CheckNamespace

using System.IO;
using System.Net;
using NServiceBus;

namespace ExactlyOnce.AcceptanceTests.Infrastructure
{
    using System.Text;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class JsonMachineInterfaceContextExtensions
    {
        static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false);
        private static readonly JsonSerializer serializer = JsonSerializer.CreateDefault();

        public static Task InvokeRest(this IMessageHandlerContext context, string urlTemplate, object request,
            object followUpMessage)
        {
            return MachineInterfaceContextExtensions.InvokeRest(context, urlTemplate, CreateHttpContent(request), followUpMessage);
        }

        public static (T, HttpStatusCode) GetResponse<T>(this IMessageHandlerContext context)
        {
            T response = default;
            var rawResponse = context.GetResponse();
            if (rawResponse.Body != null)
            {
                using var streamReader = new StreamReader(rawResponse.Body, Utf8Encoding);
                using var jsonTextReader = new JsonTextReader(streamReader);
                response = serializer.Deserialize<T>(jsonTextReader);
            }

            return (response, rawResponse.Status);
        }

        static HttpContent CreateHttpContent(object request)
        {
            return new StringContent(JsonConvert.SerializeObject(request), Utf8Encoding, "application/json");
        }
    }
}