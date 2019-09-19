using System;

namespace El.ServiceBus
{
    public delegate void OnMessageReceived(object source, MessageReceivedArgs e);

    public class MessageReceivedArgs : EventArgs
    {
        public MessageReceivedArgs(string messageEvent, DateTimeOffset publishedAt, DateTimeOffset receivedAt)
        {
            MessageEvent = messageEvent;
            PublishedAt = publishedAt;
            ReceivedAt = receivedAt;
        }

        public string MessageEvent { get; }
        public DateTimeOffset PublishedAt { get; }
        public DateTimeOffset ReceivedAt { get; }
    }
}
