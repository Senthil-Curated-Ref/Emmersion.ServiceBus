using System;

namespace El.ServiceBus
{
    internal class MessageEnvelope<T>
    {
        public string EventName { get; set; }
        public uint EventVersion { get; set; }
        public T Payload { get; set; }
        public DateTimeOffset PublishedAt { get; } = DateTimeOffset.UtcNow;
    }
}
