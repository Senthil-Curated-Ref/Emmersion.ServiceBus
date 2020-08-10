using System.Text;

namespace EL.ServiceBus
{
    internal interface IMessageMapper
    {
        Microsoft.Azure.ServiceBus.Message ToServiceBusMessage<T>(Message<T> message);
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
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message(bytes);
            serviceBusMessage.MessageId = message.MessageId;
            serviceBusMessage.CorrelationId = message.CorrelationId;
            return serviceBusMessage;
        }
    }
}
