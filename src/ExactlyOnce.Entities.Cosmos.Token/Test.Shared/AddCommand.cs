using System;
using NServiceBus;

namespace Test.Shared
{
    public class AddCommand : ICommand
    {
        public string AccountNumber { get; set; }
        public int Change { get; set; }
    }
}
