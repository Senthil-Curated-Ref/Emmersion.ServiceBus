using System;

namespace EL.ServiceBus
{
    public delegate void OnMessageReceived(object source, MessageReceivedArgs args);

    public class MessageReceivedArgs : EventArgs
    {
        public MessageReceivedArgs(Subscription subscription,
            DateTimeOffset? publishedAt,
            DateTimeOffset? enqueuedAt,
            DateTimeOffset receivedAt,
            TimeSpan processingTime)
        {
            Subscription = subscription;
            PublishedAt = publishedAt;
            EnqueuedAt = enqueuedAt;
            ReceivedAt = receivedAt;
            ProcessingTime = processingTime;
        }

        public MessageReceivedArgs(string messageEvent,
            DateTimeOffset? publishedAt,
            DateTimeOffset receivedAt,
            TimeSpan processingTime)
        {
            MessageEvent = messageEvent;
            PublishedAt = publishedAt;
            EnqueuedAt = publishedAt;
            ReceivedAt = receivedAt;
            ProcessingTime = processingTime;
        }

        public Subscription Subscription { get; }
        public string MessageEvent { get; }
        public DateTimeOffset? PublishedAt { get; }
        public DateTimeOffset? EnqueuedAt { get; }
        public DateTimeOffset ReceivedAt { get; }
        public TimeSpan ProcessingTime { get; }
    }
}
