using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NServiceBus;

namespace Test.Backend
{
    public class DebitCommandHandler :
            IHandleMessages<DebitCommand>,
            IHandleMessages<DebitCompleteCommand>
    { 
        public async Task Handle(DebitCommand message, IMessageHandlerContext context)
        {
            var content = new Dictionary<string, string>
            {
                ["debit-value"] = message.Change.ToString()
            };

            await context.InvokeRest("http://localhost:58119/debit?rid={uniqueId}&account=" + message.AccountNumber, new FormUrlEncodedContent(content), 
                new DebitCompleteCommand
            {
                AccountNumber = message.AccountNumber
            });
        }

        public Task Handle(DebitCompleteCommand message, IMessageHandlerContext context)
        {
            var response = context.GetResponse();
            if (response.Status == HttpStatusCode.OK)
            {
                Console.WriteLine("Debit succeeded");
            }
            else
            {
                Console.WriteLine($"Debit failed: {response.Status}");
            }
            return Task.CompletedTask;
        }
    }
}