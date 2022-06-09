namespace ExactlyOnce.NServiceBus
{
    using global::NServiceBus;
    using global::NServiceBus.Features;

    class LocalAddressCaptureFeature : Feature
    {
        public LocalAddressCaptureFeature()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.RegisterSingleton(new LocalAddressHolder(context.Settings.LocalAddress()));
        }
    }
}