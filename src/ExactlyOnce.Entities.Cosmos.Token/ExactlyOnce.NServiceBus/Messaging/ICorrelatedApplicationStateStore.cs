using System;
using System.Collections.Generic;
using ExactlyOnce.Core;

namespace ExactlyOnce.NServiceBus.Messaging
{
    interface ICorrelatedApplicationStateStore
    {
        ITransactionRecordContainer Create(Type messageType, Dictionary<string, string> messageHeaders, object messageBody);
    }
}