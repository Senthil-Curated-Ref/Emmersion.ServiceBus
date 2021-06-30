using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Emmersion.ServiceBus.Pools;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus
{
    public interface IMessagePublisher
    {
        [Obsolete("Use PublishAsync instead")]
        void Publish<T>(Message<T> message);
        
        Task PublishAsync<T>(Message<T> message);
        
        [Obsolete("Use PublishScheduledAsync instead")]
        void PublishScheduled<T>(Message<T> message, DateTimeOffset enqueueAt);
        
        Task PublishScheduledAsync<T>(Message<T> message, DateTimeOffset enqueueAt);
        
        [Obsolete("Use PublishAsync instead")]
        void Publish<T>(MessageEvent messageEvent, T message);
        
        Task PublishAsync<T>(MessageEvent messageEvent, T message);
        
        event OnMessagePublished OnMessagePublished;
    }

    internal class MessagePublisher : IMessagePublisher
    {
        private readonly IServiceBusSenderPool pool;
        private readonly IMessageMapper messageMapper;
        private readonly IPublisherConfig config;

        public event OnMessagePublished OnMessagePublished;

        public MessagePublisher(IServiceBusSenderPool serviceBusSenderPool,
            IMessageMapper messageMapper,
            IPublisherConfig config)
        {
            pool = serviceBusSenderPool;
            this.messageMapper = messageMapper;
            this.config = config;
        }

        [Obsolete("Use PublishAsync instead")]
        public void Publish<T>(Message<T> message)
        {
            PublishAsync(message).Wait();
        }
        
        public async Task PublishAsync<T>(Message<T> message)
        {
            await Publish(message, null, (client, data) => client.SendAsync(data));
        }

        [Obsolete("Use PublishScheduledAsync instead")]
        public void PublishScheduled<T>(Message<T> message, DateTimeOffset enqueueAt)
        {
            PublishScheduledAsync(message, enqueueAt).Wait();
        }
        
        public async Task PublishScheduledAsync<T>(Message<T> message, DateTimeOffset enqueueAt)
        {
            await Publish(message, enqueueAt, (client, data) => client.ScheduleMessageAsync(data, enqueueAt));
        }

        private async Task Publish<T>(Message<T> message, DateTimeOffset? enqueueAt, Func<IServiceBusSender, ServiceBusMessage, Task> sendTask)
        {
            var sender = await pool.GetForTopicAsync(message.Topic);
            var stopwatch = Stopwatch.StartNew();
            await sendTask(sender, PrepareMessage(message, enqueueAt));
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }

        private ServiceBusMessage PrepareMessage<T>(Message<T> message, DateTimeOffset? enqueueAt)
        {
            message.PublishedAt = DateTimeOffset.UtcNow;
            message.EnqueuedAt = enqueueAt ?? message.PublishedAt;
            message.Environment = config.Environment;
            return messageMapper.ToServiceBusMessage(message);
        }

        [Obsolete("Use PublishAsync instead")]
        public void Publish<T>(MessageEvent messageEvent, T message)
        {
            PublishAsync(messageEvent, message).Wait();
        }
        
        public async Task PublishAsync<T>(MessageEvent messageEvent, T message)
        {
            var sender = await pool.GetForSingleTopicAsync();
            var stopwatch = Stopwatch.StartNew();
            await sender.SendAsync(PrepareMessage(messageEvent, message));
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }

        private ServiceBusMessage PrepareMessage<T>(MessageEvent messageEvent, T message)
        {
            var envelope = new MessageEnvelope<T> {
                MessageEvent = messageEvent.ToString(),
                Payload = message
            };
            return messageMapper.FromMessageEnvelope(envelope);
        }
    }
}
