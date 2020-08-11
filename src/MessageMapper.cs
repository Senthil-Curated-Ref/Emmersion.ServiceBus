using System.Text;

namespace EL.ServiceBus
{
    internal interface IMessageMapper
    {
        Microsoft.Azure.ServiceBus.Message ToServiceBusMessage<T>(Message<T> message);
        Message<T> FromServiceBusMessage<T>(Topic topic, Microsoft.Azure.ServiceBus.Message message);
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
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(message.Body));
            return new Microsoft.Azure.ServiceBus.Message(bytes)
            {
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId
            };
        }

        public Message<T> FromServiceBusMessage<T>(Topic topic, Microsoft.Azure.ServiceBus.Message message)
        {
            var body = serializer.Deserialize<T>(Encoding.UTF8.GetString(message.Body));
            return new Message<T>(message.MessageId, topic, body)
            {
                CorrelationId = message.CorrelationId
            };
        }
    }
}
