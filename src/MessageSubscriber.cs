using System;
using System.Collections.Generic;
using System.Linq;

namespace El.ServiceBus
{
    public interface IMessageSubscriber
    {
        void Subscribe<T>(string eventName, uint version, Action<T> action);
        void RouteMessage(string serializedMessage);
    }

    internal class MessageSubscriber : IMessageSubscriber
    {
        private readonly List<Subscription> subscriptions = new List<Subscription>();
        private readonly IMessageSerializer messageSerializer;

        public MessageSubscriber(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        public void Subscribe<T>(string eventName, uint version, Action<T> action)
        {
            subscriptions.Add(new Subscription{
                EventName = eventName,
                Version = version,
                Action = (serializedMessage) => {
                    var envelope = messageSerializer.Deserialize<MessageEnvelope<T>>(serializedMessage);
                    action(envelope.Payload);
                }
            });
        }

        public void RouteMessage(string serializedMessage)
        {
            var envelope = messageSerializer.Deserialize<MessageEnvelope<Stub>>(serializedMessage);
            var recipients = subscriptions.Where(x => x.EventName == envelope.EventName && x.Version == envelope.EventVersion).ToList();
            System.Console.WriteLine($"Routing message to {recipients.Count} recipients: {serializedMessage}");
            recipients.ForEach(x => x.Action(serializedMessage));
        }
    }

    internal class Subscription
    {
        public string EventName { get; set; }
        public uint Version { get; set; }
        public Action<string> Action { get; set; }
    }

    internal class Stub
    {

    }
}
