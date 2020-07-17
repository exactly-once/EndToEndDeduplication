using ExactlyOnce.Core;
using NServiceBus.Logging;

namespace ExactlyOnce.NServiceBus.Web.MachineInterface
{
    class NServiceBusDebugLogger : IDebugLogger
    {
        static readonly ILog log = LogManager.GetLogger("ExactlyOnceProcessor");

        public void Log(string message)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(message);
            }
        }
    }
}