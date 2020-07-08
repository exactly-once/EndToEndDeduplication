using NServiceBus;

namespace Test.Backend
{
    public class DebitCommand : ICommand
    {
        public string AccountNumber { get; set; }
        public int Change { get; set; }
    }
}