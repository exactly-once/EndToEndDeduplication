namespace ExactlyOnce.NServiceBus
{
    class LocalAddressHolder
    {
        public LocalAddressHolder(string localAddress)
        {
            LocalAddress = localAddress;
        }

        public string LocalAddress { get; }
    }
}