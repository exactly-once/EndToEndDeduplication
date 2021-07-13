using NServiceBus;

namespace Test.Backend
{
    public class DebitCompleteCommand : ICommand
    {
        public string AccountNumber { get; set; }
    }
}