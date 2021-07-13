using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace ExactlyOnce.NServiceBus.Cosmos
{
    class RequestCorrelation
    {
        readonly string[] requiredParameters;
        readonly Func<IEnumerable<KeyValuePair<string, StringValues>>, string> correlationFunction;

        public RequestCorrelation(Func<IEnumerable<KeyValuePair<string, StringValues>>, string> correlationFunction, string[] requiredParameters)
        {
            this.correlationFunction = correlationFunction;
            this.requiredParameters = requiredParameters;
        }

        public bool HasRequiredParameters(IEnumerable<KeyValuePair<string, StringValues>> queryString)
        {
            return requiredParameters.All(p => queryString.Any(x => x.Key == p));
        }

        public string GetPartitionKey(IEnumerable<KeyValuePair<string, StringValues>> queryString)
        {
            return correlationFunction(queryString);
        }
    }
}