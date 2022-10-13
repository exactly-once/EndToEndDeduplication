using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ExactlyOnce.NServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

namespace ExactlyOnce.AcceptanceTests.Infrastructure;

public class MachineInterfaceComponentBehavior : EndpointBehavior
{
    public MachineInterfaceComponentBehavior(IEndpointConfigurationFactory endpointBuilder)
    : base(endpointBuilder)
    {
        ConfigureHowToCreateInstance(config =>
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(collection =>
                {
                    collection.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    collection.AddScoped<IMachineInterfaceConnectorMessageSession>(sp =>
                    {
                        var contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                        var context = contextAccessor.HttpContext;
                        return (IMachineInterfaceConnectorMessageSession)context.Items["session"];
                    });
                    
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup(ctx => new Startup(ctx.Configuration, endpointBuilder.GetType()));
                    webBuilder.UseUrls("http://localhost:57942");
                })
                .UseNServiceBus(context => config);

            return Task.FromResult(host.Build());

        }, async host =>
        {
            await host.StartAsync();
            var session = host.Services.GetRequiredService<IMessageSession>();
            return new ProxyEndpointInstance(session, host);
        });
    }

    public class Startup
    {
        readonly Type endpointBuilderType;

        public Startup(IConfiguration configuration, Type endpointBuilderType)
        {
            this.endpointBuilderType = endpointBuilderType;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Add(new NestedControllerFeatureProvider(endpointBuilderType));
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (!string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.EnableBuffering();
                    await next();
                    return;
                }
                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class NestedControllerFeatureProvider : ControllerFeatureProvider
    {
        Type endpointBuilderType;

        public NestedControllerFeatureProvider(Type endpointBuilderType)
        {
            this.endpointBuilderType = endpointBuilderType;
        }

        protected override bool IsController(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            if (!typeInfo.IsNested)
            {
                return false;
            }

            if (typeInfo.DeclaringType != endpointBuilderType)
            {
                return false;
            }
            
            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.IsDefined(typeof(NonControllerAttribute)))
            {
                return false;
            }

            return true;
        }
    }

    class ProxyEndpointInstance : IEndpointInstance
    {
        private IMessageSession session;
        private IHost host;

        public ProxyEndpointInstance(IMessageSession session, IHost host)
        {
            this.session = session;
            this.host = host;
        }

        public Task Send(object message, SendOptions options)
        {
            return session.Send(message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return session.Send(messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return session.Publish(message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return session.Publish(messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return session.Subscribe(eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return session.Unsubscribe(eventType, options);
        }

        public async Task Stop()
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

}

