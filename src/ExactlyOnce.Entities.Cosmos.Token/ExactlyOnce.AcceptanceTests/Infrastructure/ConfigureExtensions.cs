namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using Configuration.AdvancedExtensibility;

    public static class ConfigureExtensions
    {
        public static RoutingSettings ConfigureRouting(this EndpointConfiguration configuration) =>
            new RoutingSettings(configuration.GetSettings());

        public static ExactlyOnceEntitiesSettings<T> ConfigureTokenBasedDeduplication<T>(this EndpointConfiguration configuration)
        {
            if (!configuration.GetSettings().TryGet<ExactlyOnceEntitiesSettings<T>>("ExactlyOnce.Settings", out var settings))
            {
                settings = new ExactlyOnceEntitiesSettings<T>();
                configuration.GetSettings().Set("ExactlyOnce.Settings", settings);
            }

            return settings;
        }
    }
}