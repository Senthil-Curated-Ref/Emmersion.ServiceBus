using System;

namespace El.ServiceBus
{
    public delegate void OnMessageReceived(object source, MessageReceivedArgs e);

    public class MessageReceivedArgs : EventArgs
    {
        public MessageReceivedArgs(string eventName, uint eventVersion, DateTimeOffset publishedAt, DateTimeOffset receivedAt)
        {
            EventName = eventName;
            EventVersion = eventVersion;
            PublishedAt = publishedAt;
            ReceivedAt = receivedAt;
        }

        public string EventName { get; }
        public uint EventVersion { get; }
        public DateTimeOffset PublishedAt { get; }
        public DateTimeOffset ReceivedAt { get; }
    }
}
