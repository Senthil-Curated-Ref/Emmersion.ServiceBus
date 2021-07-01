using System;
using System.Text;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus
{
    internal interface IMessageMapper
    {
        ServiceBusMessage ToServiceBusMessage<T>(Message<T> message);
        Message<T> FromServiceBusMessage<T>(Topic topic, ServiceBusReceivedMessage message, DateTimeOffset receivedAt);
        DeadLetter GetDeadLetter(ServiceBusReceivedMessage message);
        ServiceBusMessage FromMessageEnvelope<T>(MessageEnvelope<T> envelope);
        MessageEnvelope<T> ToMessageEnvelope<T>(ServiceBusReceivedMessage message);
    }

    internal class MessageMapper : IMessageMapper
    {
        private readonly IMessageSerializer serializer;

        public MessageMapper(IMessageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public ServiceBusMessage ToServiceBusMessage<T>(Message<T> message)
        {
            var payload = new Payload<T> {
                Body = message.Body,
                PublishedAt = message.PublishedAt.Value,
                EnqueuedAt = message.EnqueuedAt.Value,
                Environment = message.Environment
            };
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(payload));
            return new ServiceBusMessage(bytes)
            {
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId
            };
        }

        public Message<T> FromServiceBusMessage<T>(Topic topic, ServiceBusReceivedMessage message, DateTimeOffset receivedAt)
        {
            var payload = serializer.Deserialize<Payload<T>>(Encoding.UTF8.GetString(message.Body));
            return new Message<T>(message.MessageId, topic, payload.Body)
            {
                CorrelationId = message.CorrelationId,
                PublishedAt = payload.PublishedAt,
                EnqueuedAt = payload.EnqueuedAt,
                ReceivedAt = receivedAt,
                Environment = payload.Environment
            };
        }

        public DeadLetter GetDeadLetter(ServiceBusReceivedMessage message) {
            return new DeadLetter
            {
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId,
                Body = Encoding.UTF8.GetString(message.Body)
            };
        }

        public ServiceBusMessage FromMessageEnvelope<T>(MessageEnvelope<T> envelope)
        {
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            return new ServiceBusMessage(bytes);
        }

        public MessageEnvelope<T> ToMessageEnvelope<T>(ServiceBusReceivedMessage message)
        {
            return serializer.Deserialize<MessageEnvelope<T>>(Encoding.UTF8.GetString(message.Body));
        }
    }

    internal class Payload<T>
    {
        public T Body { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public DateTimeOffset EnqueuedAt { get; set; }
        public string Environment { get; set; }
    }
}
