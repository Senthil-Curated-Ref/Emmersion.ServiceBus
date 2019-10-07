using System;
using System.Collections.Generic;
using System.Linq;

namespace EL.ServiceBus
{
    public interface IMessageSubscriber
    {
        void Subscribe<T>(MessageEvent messageEvent, Action<T> action);
        void RouteMessage(string serializedMessage);
        event OnMessageReceived OnMessageReceived;
    }

    internal class MessageSubscriber : IMessageSubscriber
    {
        private readonly List<Subscription> subscriptions = new List<Subscription>();
        private readonly IMessageSerializer messageSerializer;
        public event OnMessageReceived OnMessageReceived;

        public MessageSubscriber(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        public void Subscribe<T>(MessageEvent messageEvent, Action<T> action)
        {
            subscriptions.Add(new Subscription{
                MessageEvent = messageEvent.ToString(),
                Action = (serializedMessage) => {
                    var envelope = messageSerializer.Deserialize<MessageEnvelope<T>>(serializedMessage);
                    action(envelope.Payload);
                }
            });
        }

        public void RouteMessage(string serializedMessage)
        {
            var receivedAt = DateTimeOffset.UtcNow;
            var envelope = messageSerializer.Deserialize<MessageEnvelope<Stub>>(serializedMessage);
            var recipients = subscriptions.Where(x => x.MessageEvent == envelope.MessageEvent).ToList();
            recipients.ForEach(x => x.Action(serializedMessage));
            OnMessageReceived?.Invoke(this, new MessageReceivedArgs(envelope.MessageEvent, envelope.PublishedAt, receivedAt));
        }
    }

    internal class Subscription
    {
        public string MessageEvent { get; set; }
        public Action<string> Action { get; set; }
    }

    internal class Stub
    {

    }
}
