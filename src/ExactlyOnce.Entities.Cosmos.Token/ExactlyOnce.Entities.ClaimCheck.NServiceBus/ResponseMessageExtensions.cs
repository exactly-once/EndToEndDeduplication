using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public static class ResponseMessageExtensions
    {
        static readonly bool bufferContent = true;

        //public static async Task<HttpResponseMessage> Deserialize(this ResponseMessage serializedMessage)
        //{
        //    var response = new HttpResponseMessage
        //    {
        //        Content = new StreamContent(serializedMessage.SerializedResponse)
        //    };
        //    response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
        //    var responseMessage = await response.Content.ReadAsHttpResponseMessageAsync().ConfigureAwait(false);
        //    if (responseMessage.Content != null && bufferContent)
        //    {
        //        await responseMessage.Content.LoadIntoBufferAsync().ConfigureAwait(false);
        //    }
        //    return responseMessage;
        //}

        //public static async Task<OutboxRecord> ToOutboxRecord(this HttpResponseMessage response, string requestId)
        //{
        //    var metadata = new Dictionary<string, string>
        //    {
        //        ["RequestId"] = requestId
        //    };

        //    if (response.Content != null && bufferContent)
        //    {
        //        await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
        //    }

        //    var httpMessageContent = new HttpMessageContent(response);
        //    var buffer = await httpMessageContent.ReadAsByteArrayAsync();

        //    byte[] binaryData;
        //    using (var memStream = new MemoryStream())
        //    {
        //        memStream.Write(buffer, 0, buffer.Length);
        //        memStream.Flush();

        //        binaryData = memStream.ToArray();
        //    }

        //    return new OutboxRecord("HttpResponse", null, binaryData, metadata);
        //}

        public static void UpdateFromStore(this HttpResponse response, ResponseMessage storedResponse)
        {

        }

        public static ResponseMessage ToResponseMessage(this OutboxRecord outboxRecord)
        {
            return new ResponseMessage
            {
                StatusCode = int.Parse(outboxRecord.Metadata["StatusCode"])
            };
        }

        public static OutboxRecord ToOutboxRecord(this HttpResponse response, string requestId)
        {
            var metadata = new Dictionary<string, string>
            {
                ["RequestId"] = requestId,
                ["StatusCode"] = response.StatusCode.ToString()
            };

            return new OutboxRecord("HttpResponse", new Dictionary<string, string>(), new byte[0], metadata);
        }

    }
}