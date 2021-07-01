using System;

namespace Emmersion.ServiceBus
{
    public class Message<T>
    {
        public string MessageId { get; } = Guid.NewGuid().ToString();
        public string CorrelationId { get; set; } = "";
        public Topic Topic { get; }
        public T Body { get; }
        public DateTimeOffset? PublishedAt { get; internal set; }
        public DateTimeOffset? EnqueuedAt { get; internal set; }
        public DateTimeOffset? ReceivedAt { get; internal set; }
        public string Environment { get; internal set; }

        public Message(Topic topic, T body)
        {
            Topic = topic;
            Body = body;
        }

        [Obsolete("Use TestMessageBuilder to set protected fields for testing")]
        public Message(Topic topic, T body, DateTimeOffset? publishedAt, DateTimeOffset? enqueuedAt, DateTimeOffset? receivedAt)
        {
            Topic = topic;
            Body = body;
            PublishedAt = publishedAt;
            EnqueuedAt = enqueuedAt;
            ReceivedAt = receivedAt;
        }

        internal Message(string messageId, Topic topic, T body)
        {
            MessageId = messageId;
            Topic = topic;
            Body = body;
        }
    }
}
