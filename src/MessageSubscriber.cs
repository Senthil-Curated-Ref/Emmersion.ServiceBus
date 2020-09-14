using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    public interface IMessageSubscriber : IDisposable
    {
        void Subscribe<T>(Subscription subscription, Action<Message<T>> action);
        void SubscribeToDeadLetters(Subscription subscription, Action<string> action);
        void Subscribe<T>(MessageEvent messageEvent, Action<T> action);
        event OnMessageReceived OnMessageReceived;
        event OnServiceBusException OnServiceBusException;
    }

    internal class MessageSubscriber : IMessageSubscriber
    {
        private readonly ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator;
        private readonly IMessageMapper messageMapper;
        private Dictionary<string, ISubscriptionClientWrapper> clients;
        private readonly List<Route> routes = new List<Route>();
        private static object threadLock = new object();

        public event OnMessageReceived OnMessageReceived;
        public event OnServiceBusException OnServiceBusException;

        public MessageSubscriber(ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator,
            IMessageMapper messageMapper)
        {
            this.subscriptionClientWrapperCreator = subscriptionClientWrapperCreator;
            this.messageMapper = messageMapper;

            clients = new Dictionary<string, ISubscriptionClientWrapper>();
        }

        public void Subscribe<T>(Subscription subscription, Action<Message<T>> action)
        {
            var client = GetClient(subscription);
            client.RegisterMessageHandler((serviceBusMessage) => {
                var receivedAt = DateTimeOffset.UtcNow;
                DateTimeOffset? publishedAt = null;
                DateTimeOffset? enqueuedAt = null;
                try {
                    var message = messageMapper.FromServiceBusMessage<T>(subscription.Topic, serviceBusMessage, receivedAt);
                    publishedAt = message.PublishedAt;
                    enqueuedAt = message.EnqueuedAt;
                    action(message);
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
            }, (args) => OnServiceBusException?.Invoke(this, new ServiceBusExceptionArgs(subscription, args)));
        }

        public void SubscribeToDeadLetters(Subscription subscription, Action<string> action)
        {
            var deadLetterSubscription = subscription.GetDeadLetterQueue();
            var client = GetClient(deadLetterSubscription);
            client.RegisterMessageHandler(
                (serviceBusMessage) => action(messageMapper.GetDeadLetterBody(serviceBusMessage)),
                (args) => OnServiceBusException?.Invoke(this, new ServiceBusExceptionArgs(deadLetterSubscription, args)));
        }

        private ISubscriptionClientWrapper GetClient(Subscription subscription)
        {
            lock (threadLock)
            {
                if (clients.ContainsKey(subscription.ToString()))
                {
                    throw new Exception("Connecting to the same subscription twice is not allowed.");
                }
                var client = subscriptionClientWrapperCreator.Create(subscription);
                clients[subscription.ToString()] = client;
                return client;
            }
        }

        public void Dispose()
        {
            Task.WaitAll(clients.Select(x => x.Value.CloseAsync()).ToArray());
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
            lock (threadLock)
            {
                if (!clients.ContainsKey("single-topic"))
                {
                    clients["single-topic"] = subscriptionClientWrapperCreator.CreateSingleTopic();
                    clients["single-topic"].RegisterMessageHandler(RouteMessage,
                        (args) => OnServiceBusException?.Invoke(this, new ServiceBusExceptionArgs(null, args)));
                }
            }
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
