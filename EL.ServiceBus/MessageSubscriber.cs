using System;
using System.Collections.Generic;
using System.Linq;

namespace EL.ServiceBus
{
    public interface IMessageSubscriber
    {
        void Subscribe<T>(Subscription subscription, Action<Message<T>> action);
        void SubscribeToDeadLetters(Subscription subscription, Action<DeadLetter> action);
        void Subscribe<T>(MessageEvent messageEvent, Action<T> action);
        event OnMessageReceived OnMessageReceived;
        event OnException OnException;
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

        public void Subscribe<T>(Subscription subscription, Action<Message<T>> action)
        {
            var filteringDisabled = string.IsNullOrEmpty(config.EnvironmentFilter);
            var client = subscriptionClientWrapperPool.GetClient(subscription);
            client.RegisterMessageHandler((serviceBusMessage) => {
                var receivedAt = DateTimeOffset.UtcNow;
                DateTimeOffset? publishedAt = null;
                DateTimeOffset? enqueuedAt = null;
                try {
                    var message = messageMapper.FromServiceBusMessage<T>(subscription.Topic, serviceBusMessage, receivedAt);
                    publishedAt = message.PublishedAt;
                    enqueuedAt = message.EnqueuedAt;
                    if (filteringDisabled || message.Environment == config.EnvironmentFilter)
                    {
                        action(message);
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

        public void SubscribeToDeadLetters(Subscription subscription, Action<DeadLetter> action)
        {
            var client = subscriptionClientWrapperPool.GetDeadLetterClient(subscription);
            client.RegisterMessageHandler(
                (serviceBusMessage) => action(messageMapper.GetDeadLetter(serviceBusMessage)),
                (args) => OnException?.Invoke(this, new ExceptionArgs(subscription, args)));
        }

        public void Subscribe<T>(MessageEvent messageEvent, Action<T> action)
        {
            InitializeSingleTopicClient();
            routes.Add(new Route
            {
                MessageEvent = messageEvent.ToString(),
                Action = (serviceBusMessage) =>
                {
                    var envelope = messageMapper.ToMessageEnvelope<T>(serviceBusMessage);
                    action(envelope.Payload);
                }
            });
        }

        private void InitializeSingleTopicClient()
        {
            var client = subscriptionClientWrapperPool.GetSingleTopicClientIfFirstTime();
            client?.RegisterMessageHandler(RouteMessage,
                (args) => OnException?.Invoke(this, new ExceptionArgs(null, args)));
        }

        internal void RouteMessage(Microsoft.Azure.ServiceBus.Message serviceBusMessage)
        {
            var receivedAt = DateTimeOffset.UtcNow;
            var envelope = messageMapper.ToMessageEnvelope<object>(serviceBusMessage);
            var recipients = routes.Where(x => x.MessageEvent == envelope.MessageEvent).ToList();
            try
            {
                recipients.ForEach(x => x.Action(serviceBusMessage));
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
        public Action<Microsoft.Azure.ServiceBus.Message> Action { get; set; }
    }
}
