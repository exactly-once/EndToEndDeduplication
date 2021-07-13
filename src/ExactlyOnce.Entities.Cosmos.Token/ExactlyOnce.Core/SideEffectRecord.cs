using System;

namespace ExactlyOnce.Core
{
    public abstract class SideEffectRecord
    {
        public string IncomingId { get; set; }
        public Guid AttemptId { get; set; }
    }
}