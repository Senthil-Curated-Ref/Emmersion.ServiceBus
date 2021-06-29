using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Emmersion.ServiceBus.Pools;

namespace Emmersion.ServiceBus
{
    public interface IMessageSubscriber
    {
        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(Subscription subscription, Func<Message<T>, Task> messageHandler);
        
        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(Subscription subscription, Action<Message<T>> messageHandler);

        Task SubscribeAsync<T>(Subscription subscription, Func<Message<T>, Task> messageHandler);

        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(MessageEvent messageEvent, Action<T> messageHandler);
        
        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(MessageEvent messageEvent, Func<T, Task> messageHandler);

        Task SubscribeAsync<T>(MessageEvent messageEvent, Func<T, Task> messageHandler);
        
        event OnMessageReceived OnMessageReceived;
        
        event OnException OnException;
        
        [Obsolete("Use SubscribeToDeadLettersAsync instead")]
        void SubscribeToDeadLetters(Subscription subscription, Func<DeadLetter, Task> messageHandler);
        
        [Obsolete("Use SubscribeToDeadLettersAsync instead")]
        void SubscribeToDeadLetters(Subscription subscription, Action<DeadLetter> messageHandler);
        
        Task SubscribeToDeadLettersAsync(Subscription subscription, Func<DeadLetter, Task> messageHandler);
    }

    internal class MessageSubscriber : IMessageSubscriber
    {
        private readonly IServiceBusProcessorPool serviceBusProcessorPool;
        private readonly IMessageMapper messageMapper;
        private readonly ISubscriptionConfig config;
        private readonly List<Route> routes = new List<Route>();

        public event OnMessageReceived OnMessageReceived;
        public event OnException OnException;

        public MessageSubscriber(IServiceBusProcessorPool serviceBusProcessorPool,
            IMessageMapper messageMapper,
            ISubscriptionConfig config)
        {
            this.serviceBusProcessorPool = serviceBusProcessorPool;
            this.messageMapper = messageMapper;
            this.config = config;
        }

        [Obsolete("Use SubscribeAsync instead")]
        public void Subscribe<T>(Subscription subscription, Action<Message<T>> messageHandler)
        {
            Subscribe(subscription, (Message<T> message) =>
            {
                messageHandler(message);
                return Task.CompletedTask;
            });
        }

        [Obsolete("Use SubscribeAsync instead")]
        public void Subscribe<T>(Subscription subscription, Func<Message<T>, Task> messageHandler)
        {
            SubscribeAsync(subscription, messageHandler).Wait();
        }
        
        public async Task SubscribeAsync<T>(Subscription subscription, Func<Message<T>, Task> messageHandler)
        {
            var filteringDisabled = string.IsNullOrEmpty(config.EnvironmentFilter);
            var processor = await serviceBusProcessorPool.GetProcessorAsync(subscription);
            await processor.RegisterMessageHandlerAsync(async (args) => {
                var receivedAt = DateTimeOffset.UtcNow;
                DateTimeOffset? publishedAt = null;
                DateTimeOffset? enqueuedAt = null;
                try {
                    var message = messageMapper.FromServiceBusMessage<T>(subscription.Topic, args.Message, receivedAt);
                    publishedAt = message.PublishedAt;
                    enqueuedAt = message.EnqueuedAt;
                    if (filteringDisabled || message.Environment == config.EnvironmentFilter)
                    {
                        await messageHandler(message);
                    }
                }
                finally
                {
                    var processingTime = DateTimeOffset.UtcNow - receivedAt;
                    OnMessageReceived?.Invoke(this, new MessageReceivedArgs(
                        subscription,
                        publishedAt,
                        enqueuedAt,
                        receivedAt,
                        processingTime
                    ));
                }
            }, (args) =>
            {
                OnException?.Invoke(this, new ExceptionArgs(subscription, args));
                return Task.CompletedTask;
            });
        }

        [Obsolete("Use SubscribeToDeadLetters instead")]
        public void SubscribeToDeadLetters(Subscription subscription, Action<DeadLetter> messageHandler)
        {
            SubscribeToDeadLetters(subscription, (message) =>
            {
                messageHandler(message);
                return Task.CompletedTask;
            });
        }

        [Obsolete("Use SubscribeToDeadLetters instead")]
        public void SubscribeToDeadLetters(Subscription subscription, Func<DeadLetter, Task> messageHandler)
        {
            SubscribeToDeadLettersAsync(subscription, messageHandler).Wait();
        }
        
        public async Task SubscribeToDeadLettersAsync(Subscription subscription, Func<DeadLetter, Task> messageHandler)
        {
            var processor = await serviceBusProcessorPool.GetDeadLetterProcessorAsync(subscription);
            await processor.RegisterMessageHandlerAsync(
                (args) => messageHandler(messageMapper.GetDeadLetter(args.Message)),
                (args) =>
                {
                    OnException?.Invoke(this, new ExceptionArgs(subscription, args));
                    return Task.CompletedTask;
                });
        }

        [Obsolete("Use SubscribeAsync instead")]
        public void Subscribe<T>(MessageEvent messageEvent, Action<T> messageHandler)
        {
            Subscribe(messageEvent, (T data) =>
            {
                messageHandler(data);
                return Task.CompletedTask;
            });
        }
        
        [Obsolete("Use SubscribeAsync instead")]
        public void Subscribe<T>(MessageEvent messageEvent, Func<T, Task> messageHandler)
        {
            SubscribeAsync(messageEvent, messageHandler).Wait();
        }
        
        public async Task SubscribeAsync<T>(MessageEvent messageEvent, Func<T, Task> messageHandler)
        {
            await InitializeSingleTopicClient();
            routes.Add(new Route
            {
                MessageEvent = messageEvent.ToString(),
                MessageHandler = (serviceBusMessage) =>
                {
                    var envelope = messageMapper.ToMessageEnvelope<T>(serviceBusMessage);
                    return messageHandler(envelope.Payload);
                }
            });
        }

        private async Task InitializeSingleTopicClient()
        {
            var processor = serviceBusProcessorPool.GetSingleTopicProcessorIfFirstTime();
            if (processor != null)
            {
                await processor.RegisterMessageHandlerAsync(RouteMessage,
                    (args) =>
                    {
                        OnException?.Invoke(this, new ExceptionArgs(null, args));
                        return Task.CompletedTask;
                    });
            }
        }

        internal async Task RouteMessage(ProcessMessageEventArgs args)
        {
            var serviceBusMessage = args.Message;
            var receivedAt = DateTimeOffset.UtcNow;
            var envelope = messageMapper.ToMessageEnvelope<object>(serviceBusMessage);
            var recipients = routes.Where(x => x.MessageEvent == envelope.MessageEvent);
            try
            {
                foreach (var x in recipients)
                {
                    await x.MessageHandler(serviceBusMessage);
                }
            }
            finally
            {
                var processingTime = DateTimeOffset.UtcNow - receivedAt;
                OnMessageReceived?.Invoke(this, new MessageReceivedArgs(
                    envelope.MessageEvent,
                    envelope.PublishedAt,
                    receivedAt,
                    processingTime
                ));
            }
        }
    }

    internal class Route
    {
        public string MessageEvent { get; set; }
        public Func<ServiceBusReceivedMessage, Task> MessageHandler { get; set; }
    }
}
