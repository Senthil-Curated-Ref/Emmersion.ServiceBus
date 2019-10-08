using System;

namespace EL.ServiceBus
{
    public delegate void OnMessageReceived(object source, MessageReceivedArgs e);

    public class MessageReceivedArgs : EventArgs
    {
        public MessageReceivedArgs(string messageEvent, DateTimeOffset publishedAt, DateTimeOffset receivedAt, TimeSpan processingTime)
        {
            MessageEvent = messageEvent;
            PublishedAt = publishedAt;
            ReceivedAt = receivedAt;
            ProcessingTime = processingTime;
        }

        public string MessageEvent { get; }
        public DateTimeOffset PublishedAt { get; }
        public DateTimeOffset ReceivedAt { get; }
        public TimeSpan ProcessingTime { get; }
    }
}
