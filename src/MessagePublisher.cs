using System;
using System.Diagnostics;
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
        private readonly IMessageMapper messageMapper;

        public event OnMessagePublished OnMessagePublished;

        public MessagePublisher(ITopicClientWrapperPool topicClientWrapperPool,
            IMessageMapper messageMapper)
        {
            this.pool = topicClientWrapperPool;
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
            var client = pool.GetForTopic(message.Topic);
            var stopwatch = Stopwatch.StartNew();
            action(client, PrepareMessage(message, enqueueAt)).Wait();
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }

        private Microsoft.Azure.ServiceBus.Message PrepareMessage<T>(Message<T> message, DateTimeOffset? enqueueAt)
        {
            message.PublishedAt = DateTimeOffset.UtcNow;
            message.EnqueuedAt = enqueueAt ?? message.PublishedAt;
            return messageMapper.ToServiceBusMessage(message);
        }

        public void Publish<T>(MessageEvent messageEvent, T message)
        {
            var client = pool.GetForSingleTopic();
            var stopwatch = Stopwatch.StartNew();
            client.SendAsync(PrepareMessage(messageEvent, message)).Wait();
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }

        private Microsoft.Azure.ServiceBus.Message PrepareMessage<T>(MessageEvent messageEvent, T message)
        {
            var envelope = new MessageEnvelope<T> {
                MessageEvent = messageEvent.ToString(),
                Payload = message
            };
            return messageMapper.FromMessageEnvelope(envelope);
        }
    }
}
