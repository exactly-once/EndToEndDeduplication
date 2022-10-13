using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

namespace ExactlyOnce.NServiceBus.Web
{
    class SessionCaptureFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SessionCaptureStartupTask>(DependencyLifecycle.SingleInstance);
            context.RegisterStartupTask(builder => builder.Build<SessionCaptureStartupTask>());
        }
    }

    class SessionCaptureStartupTask : FeatureStartupTask
    {
        public IMessageSession Session { get; private set; }

        protected override Task OnStart(IMessageSession session)
        {
            Session = session;
            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session)
        {
            return Task.CompletedTask;
        }
    }
}