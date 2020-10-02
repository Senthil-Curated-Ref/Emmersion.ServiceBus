using System;
using System.Text;

namespace EL.ServiceBus
{
    internal interface IMessageMapper
    {
        Microsoft.Azure.ServiceBus.Message ToServiceBusMessage<T>(Message<T> message);
        Message<T> FromServiceBusMessage<T>(Topic topic, Microsoft.Azure.ServiceBus.Message message, DateTimeOffset receivedAt);
        DeadLetter GetDeadLetter(Microsoft.Azure.ServiceBus.Message message);
        Microsoft.Azure.ServiceBus.Message FromMessageEnvelope<T>(MessageEnvelope<T> envelope);
        MessageEnvelope<T> ToMessageEnvelope<T>(Microsoft.Azure.ServiceBus.Message message);
    }

    internal class MessageMapper : IMessageMapper
    {
        private readonly IMessageSerializer serializer;

        public MessageMapper(IMessageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public Microsoft.Azure.ServiceBus.Message ToServiceBusMessage<T>(Message<T> message)
        {
            var payload = new Payload<T> {
                Body = message.Body,
                PublishedAt = message.PublishedAt.Value,
                EnqueuedAt = message.EnqueuedAt.Value
            };
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(payload));
            return new Microsoft.Azure.ServiceBus.Message(bytes)
            {
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId
            };
        }

        public Message<T> FromServiceBusMessage<T>(Topic topic, Microsoft.Azure.ServiceBus.Message message, DateTimeOffset receivedAt)
        {
            var payload = serializer.Deserialize<Payload<T>>(Encoding.UTF8.GetString(message.Body));
            return new Message<T>(message.MessageId, topic, payload.Body)
            {
                CorrelationId = message.CorrelationId,
                PublishedAt = payload.PublishedAt,
                EnqueuedAt = payload.EnqueuedAt,
                ReceivedAt = receivedAt
            };
        }

        public DeadLetter GetDeadLetter(Microsoft.Azure.ServiceBus.Message message) {
            return new DeadLetter
            {
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId,
                Body = Encoding.UTF8.GetString(message.Body)
            };
        }

        public Microsoft.Azure.ServiceBus.Message FromMessageEnvelope<T>(MessageEnvelope<T> envelope)
        {
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            return new Microsoft.Azure.ServiceBus.Message(bytes);
        }

        public MessageEnvelope<T> ToMessageEnvelope<T>(Microsoft.Azure.ServiceBus.Message message)
        {
            return serializer.Deserialize<MessageEnvelope<T>>(Encoding.UTF8.GetString(message.Body));
        }
    }

    internal class Payload<T>
    {
        public T Body { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public DateTimeOffset EnqueuedAt { get; set; }
    }
}
