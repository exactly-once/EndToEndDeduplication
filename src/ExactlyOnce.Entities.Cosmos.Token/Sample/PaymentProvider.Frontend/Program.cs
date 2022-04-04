using System.IO;
using System.Text;
using Azure.Storage.Blobs;
using ExactlyOnce.NServiceBus;
using ExactlyOnce.NServiceBus.Blob;
using ExactlyOnce.NServiceBus.Cosmos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NServiceBus;
using PaymentProvider.Contracts;
using PaymentProvider.Frontend.Controllers;

namespace PaymentProvider.Frontend
{
    using Microsoft.AspNetCore.Http;
    using Models;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var messageStore = new MessageStore(new BlobContainerClient("UseDevelopmentStorage=true", "claim-check"));
            var requestResponseStoreClient = new BlobContainerClient("UseDevelopmentStorage=true", "payment-provider-web");

            var clientOptions = new CosmosClientOptions();
            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", clientOptions);
            var appDataContainer = client.GetContainer("ExactlyOnce.Sample", "payment-provider");

            var stateStore = new ApplicationStateStore(appDataContainer, "Partition");

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(collection =>
                {
                    collection.AddSingleton(appDataContainer);
                    collection.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    collection.AddSingleton<IMachineInterfaceConnectorMessageSession<AuthorizeRequest>, ContextMachineInterfaceConnectorMessageSession<AuthorizeRequest>>();
                })
                .UseNServiceBus(context =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Samples.ExactlyOnce.PaymentProvider.Frontend");
                    endpointConfiguration.SendOnly();
                    endpointConfiguration.EnableInstallers();
                    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                    transport.ConnectionString("host=localhost");
                    transport.UseConventionalRoutingTopology();
                    var routing = transport.Routing();
                    routing.RouteToEndpoint(typeof(SettleTransaction), "Samples.ExactlyOnce.PaymentProvider.Backend");
                    return endpointConfiguration;
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseExactlyOnceWebMachineInterface(stateStore,
                    new MachineWebInterfaceRequestStore(requestResponseStoreClient, "request-"),
                    new MachineWebInterfaceResponseStore(requestResponseStoreClient, "response-"), messageStore);
        }
    }

    //public class MySetup : IConfigureOptions<MvcOptions>, IPostConfigureOptions<MvcOptions>
    //{
    //    public void Configure(MvcOptions options)
    //    {
    //        options.ModelBinderProviders.Add(new StoredRequestModelBinderProvider(options.InputFormatters);
    //    }

    //    public void PostConfigure(string name, MvcOptions options)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}

    //public class StoredRequestModelBinderProvider : IModelBinderProvider
    //{
    //    readonly FormatterCollection<IInputFormatter> formatters;

    //    public StoredRequestModelBinderProvider(FormatterCollection<IInputFormatter> formatters)
    //    {
    //        this.formatters = formatters;
    //    }

    //    public IModelBinder GetBinder(ModelBinderProviderContext context)
    //    {
    //        return new BodyModelBinder(formatters, new D());
    //    }
    //}

    //public class D : IHttpRequestStreamReaderFactory
    //{
    //    public TextReader CreateReader(Stream stream, Encoding encoding)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
}
