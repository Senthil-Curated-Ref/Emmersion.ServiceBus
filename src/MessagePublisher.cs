using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    public interface IMessagePublisher : IDisposable
    {
        void Publish<T>(Message<T> message);
        void PublishScheduled<T>(Message<T> message, DateTimeOffset enqueueAt);
        void Publish<T>(MessageEvent messageEvent, T message);
        event OnMessagePublished OnMessagePublished;
    }

    internal class MessagePublisher : IMessagePublisher
    {
        private readonly ITopicClientWrapperPool pool;
        private readonly IMessageSerializer serializer;
        private readonly IPublisherConfig publisherConfig;
        private readonly IMessageMapper messageMapper;

        public event OnMessagePublished OnMessagePublished;

        public MessagePublisher(ITopicClientWrapperPool topicClientWrapperPool,
            IMessageSerializer serializer,
            IPublisherConfig publisherConfig,
            IMessageMapper messageMapper)
        {
            this.pool = topicClientWrapperPool;
            this.serializer = serializer;
            this.publisherConfig = publisherConfig;
            this.messageMapper = messageMapper;
        }

        public void Dispose()
        {
            pool.Dispose();
        }

        public void Publish<T>(Message<T> message)
        {
            Publish(message, null, (client, data) => client.SendAsync(data));
        }

        public void PublishScheduled<T>(Message<T> message, DateTimeOffset enqueueAt)
        {
            Publish(message, enqueueAt, (client, data) => client.ScheduleMessageAsync(data, enqueueAt));
        }

        private void Publish<T>(Message<T> message, DateTimeOffset? enqueueAt, Func<ITopicClientWrapper, Microsoft.Azure.ServiceBus.Message, Task> action)
        {
            var client = pool.GetForTopic(publisherConfig.ConnectionString, message.Topic.ToString());
            message.PublishedAt = DateTimeOffset.UtcNow;
            message.EnqueuedAt = enqueueAt ?? message.PublishedAt;
            var stopwatch = Stopwatch.StartNew();
            action(client, messageMapper.ToServiceBusMessage(message)).Wait();
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }

        public void Publish<T>(MessageEvent messageEvent, T message)
        {
            var client = pool.GetForTopic(publisherConfig.SingleTopicConnectionString, publisherConfig.SingleTopicName);
            var stopwatch = Stopwatch.StartNew();
            var envelope = new MessageEnvelope<T> {
                MessageEvent = messageEvent.ToString(),
                Payload = message
            };
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            client.SendAsync(new Microsoft.Azure.ServiceBus.Message(bytes)).Wait();
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }
    }
}
