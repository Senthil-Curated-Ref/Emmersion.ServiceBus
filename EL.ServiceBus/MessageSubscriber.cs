using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    public interface IMessageSubscriber
    {
        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(Subscription subscription, Func<Message<T>, Task> messageHandler);
        
        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(Subscription subscription, Action<Message<T>> messageHandler);

        Task SubscribeAsync<T>(Subscription subscription, Func<Message<T>, Task> messageHandler);

        void Subscribe<T>(MessageEvent messageEvent, Action<T> messageHandler);
        
        void Subscribe<T>(MessageEvent messageEvent, Func<T, Task> messageHandler);
        
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
        private readonly ISubscriptionClientWrapperPool subscriptionClientWrapperPool;
        private readonly IMessageMapper messageMapper;
        private readonly ISubscriptionConfig config;
        private readonly List<Route> routes = new List<Route>();

        public event OnMessageReceived OnMessageReceived;
        public event OnException OnException;

        public MessageSubscriber(ISubscriptionClientWrapperPool subscriptionClientWrapperPool,
            IMessageMapper messageMapper,
            ISubscriptionConfig config)
        {
            this.subscriptionClientWrapperPool = subscriptionClientWrapperPool;
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
            var client = await subscriptionClientWrapperPool.GetClientAsync(subscription);
            client.RegisterMessageHandler(async (serviceBusMessage) => {
                var receivedAt = DateTimeOffset.UtcNow;
                DateTimeOffset? publishedAt = null;
                DateTimeOffset? enqueuedAt = null;
                try {
                    var message = messageMapper.FromServiceBusMessage<T>(subscription.Topic, serviceBusMessage, receivedAt);
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
            }, (args) => OnException?.Invoke(this, new ExceptionArgs(subscription, args)));
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
            var client = await subscriptionClientWrapperPool.GetDeadLetterClientAsync(subscription);
            client.RegisterMessageHandler(
                (serviceBusMessage) => messageHandler(messageMapper.GetDeadLetter(serviceBusMessage)),
                (args) => OnException?.Invoke(this, new ExceptionArgs(subscription, args)));
        }

        public void Subscribe<T>(MessageEvent messageEvent, Action<T> messageHandler)
        {
            Subscribe(messageEvent, (T data) =>
            {
                messageHandler(data);
                return Task.CompletedTask;
            });
        }
        
        public void Subscribe<T>(MessageEvent messageEvent, Func<T, Task> messageHandler)
        {
            InitializeSingleTopicClient();
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

        private void InitializeSingleTopicClient()
        {
            var client = subscriptionClientWrapperPool.GetSingleTopicClientIfFirstTime();
            client?.RegisterMessageHandler(RouteMessage,
                (args) => OnException?.Invoke(this, new ExceptionArgs(null, args)));
        }

        internal async Task RouteMessage(Microsoft.Azure.ServiceBus.Message serviceBusMessage)
        {
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
        public Func<Microsoft.Azure.ServiceBus.Message, Task> MessageHandler { get; set; }
    }
}
