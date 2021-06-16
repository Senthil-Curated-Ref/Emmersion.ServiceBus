using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    public interface IMessageSubscriber
    {
        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(Subscription subscription, Func<Message<T>, Task> action);
        
        [Obsolete("Use SubscribeAsync instead")]
        void Subscribe<T>(Subscription subscription, Action<Message<T>> action);
        
        Task SubscribeAsync<T>(Subscription subscription, Func<Message<T>, Task> action);
        
        Task SubscribeAsync<T>(Subscription subscription, Action<Message<T>> action);
        
        void Subscribe<T>(MessageEvent messageEvent, Action<T> action);
        
        void Subscribe<T>(MessageEvent messageEvent, Func<T, Task> action);
        
        event OnMessageReceived OnMessageReceived;
        
        event OnException OnException;
        
        [Obsolete("Use SubscribeToDeadLettersAsync instead")]
        void SubscribeToDeadLetters(Subscription subscription, Func<DeadLetter, Task> action);
        
        [Obsolete("Use SubscribeToDeadLettersAsync instead")]
        void SubscribeToDeadLetters(Subscription subscription, Action<DeadLetter> action);
        
        Task SubscribeToDeadLettersAsync(Subscription subscription, Func<DeadLetter, Task> action);
        
        Task SubscribeToDeadLettersAsync(Subscription subscription, Action<DeadLetter> action);
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
        public void Subscribe<T>(Subscription subscription, Action<Message<T>> action)
        {
            Subscribe(subscription, (Message<T> message) =>
            {
                action(message);
                return Task.CompletedTask;
            });
        }

        [Obsolete("Use SubscribeAsync instead")]
        public void Subscribe<T>(Subscription subscription, Func<Message<T>, Task> action)
        {
            SubscribeAsync(subscription, action).Wait();
        }
        
        public async Task SubscribeAsync<T>(Subscription subscription, Action<Message<T>> action)
        {
            await SubscribeAsync(subscription, (Message<T> message) =>
            {
                action(message);
                return Task.CompletedTask;
            });
        }
        
        public async Task SubscribeAsync<T>(Subscription subscription, Func<Message<T>, Task> action)
        {
            var filteringDisabled = string.IsNullOrEmpty(config.EnvironmentFilter);
            var client = await subscriptionClientWrapperPool.GetClient(subscription);
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
                        await action(message);
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
        public void SubscribeToDeadLetters(Subscription subscription, Action<DeadLetter> action)
        {
            SubscribeToDeadLetters(subscription, (message) =>
            {
                action(message);
                return Task.CompletedTask;
            });
        }

        [Obsolete("Use SubscribeToDeadLetters instead")]
        public void SubscribeToDeadLetters(Subscription subscription, Func<DeadLetter, Task> action)
        {
            SubscribeToDeadLettersAsync(subscription, action).Wait();
        }

        public Task SubscribeToDeadLettersAsync(Subscription subscription, Action<DeadLetter> action)
        {
            return SubscribeToDeadLettersAsync(subscription, (message) =>
            {
                action(message);
                return Task.CompletedTask;
            });
        }
        
        public async Task SubscribeToDeadLettersAsync(Subscription subscription, Func<DeadLetter, Task> action)
        {
            var client = await subscriptionClientWrapperPool.GetDeadLetterClient(subscription);
            client.RegisterMessageHandler(
                (serviceBusMessage) => action(messageMapper.GetDeadLetter(serviceBusMessage)),
                (args) => OnException?.Invoke(this, new ExceptionArgs(subscription, args)));
        }

        public void Subscribe<T>(MessageEvent messageEvent, Action<T> action)
        {
            Subscribe(messageEvent, (T data) =>
            {
                action(data);
                return Task.CompletedTask;
            });
        }
        
        public void Subscribe<T>(MessageEvent messageEvent, Func<T, Task> action)
        {
            InitializeSingleTopicClient();
            routes.Add(new Route
            {
                MessageEvent = messageEvent.ToString(),
                Action = (serviceBusMessage) =>
                {
                    var envelope = messageMapper.ToMessageEnvelope<T>(serviceBusMessage);
                    return action(envelope.Payload);
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
                    await x.Action(serviceBusMessage);
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
        public Func<Microsoft.Azure.ServiceBus.Message, Task> Action { get; set; }
    }
}
