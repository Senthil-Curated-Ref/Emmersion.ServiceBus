using System;

namespace EL.ServiceBus
{
    public class Message<T>
    {
        public string MessageId { get; private set; } = Guid.NewGuid().ToString();
        public string CorrelationId { get; set; }
        public Topic Topic { get; private set; }
        public T Body { get; private set; }

        public Message(Topic topic, T body)
        {
            Topic = topic;
            Body = body;
        }

        internal Message(string messageId, Topic topic, T body)
        {
            MessageId = messageId;
            Topic = topic;
            Body = body;
        }
    }
}
