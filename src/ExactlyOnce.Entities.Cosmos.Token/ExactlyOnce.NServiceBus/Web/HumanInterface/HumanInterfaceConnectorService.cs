namespace ExactlyOnce.NServiceBus.Web.HumanInterface
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    class HumanInterfaceConnectorService<TPartition> : IHostedService
    {
        readonly IServiceProvider serviceProvider;

        public HumanInterfaceConnectorService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var connector = serviceProvider.GetRequiredService<IHumanInterfaceConnector<TPartition>>();
            return connector.Start();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var connector = serviceProvider.GetRequiredService<IHumanInterfaceConnector<TPartition>>();
            return connector.Stop();
        }
    }
}