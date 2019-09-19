using System;

namespace El.ServiceBus
{
    internal class MessageEnvelope<T>
    {
        public string MessageEvent { get; set; }
        public T Payload { get; set; }
        public DateTimeOffset PublishedAt { get; } = DateTimeOffset.UtcNow;
    }
}
