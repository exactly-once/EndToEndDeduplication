

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    using System.Text;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class JsonMachineInterfaceContextExtensions
    {
        static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false);

        public static Task InvokeRest(this IMessageHandlerContext context, string urlTemplate, object request,
            object followUpMessage)
        {
            return context.InvokeRest(urlTemplate, CreateHttpContent(request), followUpMessage);
        }

        static HttpContent CreateHttpContent(object request)
        {
            return new StringContent(JsonConvert.SerializeObject(request), Utf8Encoding, "application/json");
        }
    }
}