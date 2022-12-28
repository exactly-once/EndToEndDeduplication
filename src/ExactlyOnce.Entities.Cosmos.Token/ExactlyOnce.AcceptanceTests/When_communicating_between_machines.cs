using System;
using System.Collections.Generic;
using System.Net;
using ExactlyOnce.AcceptanceTests.Infrastructure;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Infrastructure;
    using NUnit.Framework;
    using ObjectBuilder;


    /*
     * Failure cases when processing a message
     *  - Loading message body
     *  - Loading transaction document
     *  - Handler execution
     *  - Checking token
     *  - Commit state transition
     *  - Apply side effects
     *  - Clean transaction
     *
     */

    /*
     * Test scenarios:
     *  - machine-to-machine: verify payload processed exactly once even if failures happen on the server
     *  - machine-to-machine: verify payload delivered even if failures happen on the client
     *  - machine-to-machine: verify response can be examined on the client
     *  - human-to-machine: verify atomic-save-and-publish
     *  - message: verify exactly once processing when failure occur in the failure cases documented above
     */

    //TODO next: Make sure the test verifies all the expected side effects of a machine-to-machine call
    public class When_communicating_between_machines : NServiceBusAcceptanceTest
    {
        private static IEnumerable<TestCaseData> GetCases()
        {
            yield return new TestCaseData(new ChaosMonkeyCollection());
            yield return new TestCaseData(new ChaosMonkeyCollection(post: 1));
            yield return new TestCaseData(new ChaosMonkeyCollection(put: 1));
            yield return new TestCaseData(new ChaosMonkeyCollection(delete: 1));
        }

        [Test, TestCaseSource(nameof(GetCases))]
        public async Task Should_return_response(ChaosMonkeyCollection chaosMonkeys)
        {
            var result = await Scenario.Define<Context>(c =>
                {
                    c.ChaosMonkeys = chaosMonkeys;
                })
                .WithEndpoint<Frontend>(s => s.When(ctx => ctx.EndpointsStarted, async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    var connector = scope.Build<IHumanInterfaceConnector<string>>();

                    var partitionKey = ctx.FrontendPartitionKey;
                    await connector.ExecuteTransaction(partitionKey, async session =>
                    {
                        session.TransactionBatch().CreateItem(new MyDocument
                        {
                            Message = "Frontend",
                            PartitionKey = ctx.FrontendPartitionKey
                        });

                        await session.Send(new InitMessage
                        {
                            PartitionKey = ctx.ClientPartitionKey,
                            Message = "Frontend"
                        }).ConfigureAwait(false);
                        return true;
                    });
                }))
                .WithEndpoint<Client>()
                .WithMachineInterfaceEndpoint(new Server(), builder => { })
                .WithEndpoint<Backend>()
                .Done(c => c.FinalMessageReceived && c.FollowUpMessageReceived)
                .Run();

            Assert.IsTrue(result.InitMessageReceived);
            Assert.IsTrue(result.FinalMessageReceived);
            Assert.IsTrue(result.FollowUpMessageReceived);
        }

        class Context : ScenarioContext, IInjectBuilder, IChaosCollectionHolder
        {
            public bool InitMessageReceived { get; set; }
            public bool FollowUpMessageReceived { get; set; }
            public bool FinalMessageReceived { get; set; }
            public IBuilder Builder { get; set; }
            public HttpStatusCode ResponseCode { get; set; }
            public string ResponseText { get; set; }

            public string FrontendPartitionKey { get; set; } = Guid.NewGuid().ToString();
            public string ClientPartitionKey { get; set; } = Guid.NewGuid().ToString();
            public string ServerPartitionKey { get; set; } = Guid.NewGuid().ToString();
            public MyDocument FrontendDoc { get; set; }
            public MyDocument ClientDoc { get; set; }
            public MyDocument ServerDoc { get; set; }

            public ChaosMonkeyCollection ChaosMonkeys { get; set; }
        }

        class Frontend : EndpointConfigurationBuilder
        {
            public Frontend() => EndpointSetup<HumanInterfaceEndpoint>(endpointConfiguration =>
            {
                endpointConfiguration.ConfigureRouting().RouteToEndpoint(typeof(InitMessage), AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Client)));
            });
        }

        class Client : EndpointConfigurationBuilder
        {
            public Client() => EndpointSetup<MessagingEndpoint>(endpointConfiguration =>
            {
                endpointConfiguration.ConfigureTokenBasedDeduplication<string>()
                    .MapMessage<InitMessage>((msg, headers) => msg.PartitionKey)
                    .MapMessage<FollowUpMessage>((msg, headers) => msg.PartitionKey);
            });

            class InitHandler : IHandleMessages<InitMessage>
            {
                public InitHandler(Context testContext) => this.testContext = testContext;

                public async Task Handle(InitMessage message, IMessageHandlerContext context)
                {
                    testContext.InitMessageReceived = true;

                    var newText = message.Message + " Client";
                    context.TransactionContext().Batch().CreateItem(new MyDocument
                    {
                        Message = newText,
                        PartitionKey = message.PartitionKey
                    });

                    await context.InvokeRest("http://localhost:57942/request/{uniqueId}/"+testContext.ServerPartitionKey, 
                        new MyRequest
                        {
                            Message = newText
                        },
                        new FollowUpMessage
                        {
                            PartitionKey = message.PartitionKey
                        });
                }

                readonly Context testContext;
            }

            class FollowUpHandler : IHandleMessages<FollowUpMessage>
            {
                public FollowUpHandler(Context testContext) => this.testContext = testContext;

                public async Task Handle(FollowUpMessage message, IMessageHandlerContext context)
                {
                    var (response, status) = context.GetResponse<MyResponse>();
                    testContext.ResponseCode = status;
                    testContext.ResponseText = response?.Message;
                    testContext.FollowUpMessageReceived = true;
                }

                readonly Context testContext;
            }
        }

        class Server : EndpointConfigurationBuilder
        {
            public Server() => EndpointSetup<MachineInterfaceEndpoint>(endpointConfiguration =>
            {
                endpointConfiguration.ConfigureRouting().RouteToEndpoint(typeof(FinalMessage), AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Backend)));
            });

            [ApiController]
            public class MyController : ControllerBase
            {
                readonly IMachineInterfaceConnectorMessageSession session;
                readonly Context testContext;

                public MyController(IMachineInterfaceConnectorMessageSession session, Context testContext)
                {
                    this.session = session;
                    this.testContext = testContext;
                }

                [Route("request/{transactionId}/{partitionKey}")]
                [MachineInterface(TransactionId = "transactionId", PartitionKey = "partitionKey")]
                [Consumes("application/json")]
                [Produces("application/json")]
                public async Task<IActionResult> Authorize(string transactionId, string partitionKey, [FromBody] MyRequest request)
                {
                    var newText = request.Message + " Server";
                    await session.Send(new FinalMessage
                    {
                        PartitionKey = partitionKey,
                        Message = newText
                    });

                    session.TransactionContext.Batch().CreateItem(new MyDocument
                    {
                        Message = newText,
                        PartitionKey = partitionKey
                    });

                    return new ObjectResult(new MyResponse
                    {
                        Message = newText
                    });
                }
            }
        }

        class Backend : EndpointConfigurationBuilder
        {
            public Backend() => EndpointSetup<MessagingEndpoint>(endpointConfiguration =>
            {
                endpointConfiguration.ConfigureTokenBasedDeduplication<string>()
                    .MapMessage<FinalMessage>((msg, headers) => msg.PartitionKey);
            });

            class FinalMessageHandler : IHandleMessages<FinalMessage>
            {
                public FinalMessageHandler(Context testContext) => this.testContext = testContext;

                public async Task Handle(FinalMessage message, IMessageHandlerContext context)
                {
                    testContext.FrontendDoc = await context.TransactionBatch().TryReadItemAsync<MyDocument>("Frontend", new PartitionKey(testContext.FrontendPartitionKey));
                    testContext.ClientDoc = await context.TransactionBatch().TryReadItemAsync<MyDocument>("Frontend Client", new PartitionKey(testContext.ClientPartitionKey));
                    testContext.ServerDoc = await context.TransactionBatch().TryReadItemAsync<MyDocument>("Frontend Client Server", new PartitionKey(testContext.ServerPartitionKey));

                    testContext.FinalMessageReceived = true;
                }

                readonly Context testContext;
            }
        }

        class InitMessage : ICommand
        {
            public string PartitionKey { get; set; }
            public string Message { get; set; }
        }

        class FollowUpMessage : IMessage
        {
            public string PartitionKey { get; set; }
        }

        class FinalMessage : IMessage
        {
            public string PartitionKey { get; set; }
            public string Message { get; set; }
        }

        public class MyRequest
        {
            public string Message { get; set; }
        }

        public class MyResponse
        {
            public string Message { get; set; }
        }

        public class MyDocument
        {
            public string PartitionKey { get; set; }
            [JsonProperty("id")]
            public string Message { get; set; }

            public override string ToString()
            {
                return Message;
            }
        }
    }
}