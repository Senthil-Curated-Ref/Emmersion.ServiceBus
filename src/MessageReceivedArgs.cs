using System;

namespace EL.ServiceBus
{
    public delegate void OnMessageReceived(object source, MessageReceivedArgs args);

    public class MessageReceivedArgs : EventArgs
    {
        public MessageReceivedArgs(string messageEvent,
            DateTimeOffset publishedAt,
            DateTimeOffset receivedAt,
            TimeSpan processingTime,
            int subscriberCount)
        {
            MessageEvent = messageEvent;
            PublishedAt = publishedAt;
            ReceivedAt = receivedAt;
            ProcessingTime = processingTime;
            SubscriberCount = subscriberCount;
        }

        public string MessageEvent { get; }
        public DateTimeOffset PublishedAt { get; }
        public DateTimeOffset ReceivedAt { get; }
        public TimeSpan ProcessingTime { get; }
        public int SubscriberCount { get; }
    }
}
