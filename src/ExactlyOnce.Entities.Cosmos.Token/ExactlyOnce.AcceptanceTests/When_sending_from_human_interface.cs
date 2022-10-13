using System;
using ExactlyOnce.AcceptanceTests.Infrastructure;
using ExactlyOnce.NServiceBus;

namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Infrastructure;
    using NUnit.Framework;
    using ObjectBuilder;

    public class When_sending_from_human_interface : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_messages_when_transaction_succeeds()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<BackendEndpoint>()
                .WithEndpoint<FrontendEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    var connector = scope.Build<IHumanInterfaceConnector<string>>();

                    var partitionKey = Guid.NewGuid().ToString();
                    await connector.ExecuteTransaction(partitionKey, async session =>
                    {
                        await session.Send(new SampleMessage
                        {
                            PartitionKey = partitionKey
                        }).ConfigureAwait(false);
                        return true;
                    });
                }))
                .Done(c => c.MessageReceived)
                .Run();
        }

        class Context : ScenarioContext, IInjectBuilder
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IBuilder Builder { get; set; }
        }

        class FrontendEndpoint : EndpointConfigurationBuilder
        {
            public FrontendEndpoint() => EndpointSetup<HumanInterfaceEndpoint>(endpointConfiguration =>
            {
                endpointConfiguration.ConfigureRouting().RouteToEndpoint(typeof(SampleMessage), AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(BackendEndpoint)));
            });
        }

        class BackendEndpoint : EndpointConfigurationBuilder
        {
            public BackendEndpoint() => EndpointSetup<MessagingEndpoint>(endpointConfiguration =>
            {
                endpointConfiguration.ConfigureTokenBasedDeduplication<string>()
                    .MapMessage<SampleMessage>((msg, headers) => msg.PartitionKey);
            });

            class SampleHandler : IHandleMessages<SampleMessage>
            {
                public SampleHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }

            class CompleteTestMessageHandler : IHandleMessages<CompleteTestMessage>
            {
                public CompleteTestMessageHandler(Context context) => testContext = context;

                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        class SampleMessage : ICommand
        {
            public string PartitionKey { get; set; }
        }

        class CompleteTestMessage : ICommand
        {
        }
    }
}