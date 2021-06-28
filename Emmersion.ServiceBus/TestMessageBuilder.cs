using System;

namespace Emmersion.ServiceBus
{
    public class TestMessageBuilder<T>
    {
        private string messageId = Guid.NewGuid().ToString();
        private Topic topic = new Topic("example", "test-event", 1);
        private string correlationId = null;
        private DateTimeOffset? publishedAt = null;
        private DateTimeOffset? enqueuedAt = null;
        private DateTimeOffset? receivedAt = null;
        private string environment = null;
        
        public TestMessageBuilder<T> WithMessageId(string messageId)
        {
            this.messageId = messageId;
            return this;
        }

        public TestMessageBuilder<T> WithTopic(Topic topic)
        {
            this.topic = topic;
            return this;
        }
        
        public TestMessageBuilder<T> WithCorrelationId(string correlationId)
        {
            this.correlationId = correlationId;
            return this;
        }
        
        public TestMessageBuilder<T> WithPublishedAt(DateTimeOffset? publishedAt)
        {
            this.publishedAt = publishedAt;
            return this;
        }
        
        public TestMessageBuilder<T> WithEnqueuedAt(DateTimeOffset? enqueuedAt)
        {
            this.enqueuedAt = enqueuedAt;
            return this;
        }
        
        public TestMessageBuilder<T> WithReceivedAt(DateTimeOffset? receivedAt)
        {
            this.receivedAt = receivedAt;
            return this;
        }
        
        public TestMessageBuilder<T> WithEnvironment(string environment)
        {
            this.environment = environment;
            return this;
        }

        public Message<T> Build(T body)
        {
            return new Message<T>(messageId.ToString(), topic, body)
            {
                CorrelationId = correlationId,
                PublishedAt = publishedAt,
                EnqueuedAt = enqueuedAt,
                ReceivedAt = receivedAt,
                Environment = environment
            };
        }
    }
}