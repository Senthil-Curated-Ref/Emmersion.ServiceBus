using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    public interface IMessageSubscriber : IDisposable
    {
        void Subscribe<T>(Subscription subscription, Action<Message<T>> action);
        event OnMessageReceived OnMessageReceived;
        event OnServiceBusException OnServiceBusException;
    }

    internal class MessageSubscriber : IMessageSubscriber
    {
        private readonly ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator;
        private readonly IMessageMapper messageMapper;
        private Dictionary<string, ISubscriptionClientWrapper> clients;

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
                try {
                    var message = messageMapper.FromServiceBusMessage<T>(subscription.Topic, serviceBusMessage);
                    action(message);
                }
                finally
                {
                    var processingTime = DateTimeOffset.UtcNow - receivedAt;
                    var enqueuedAt = new DateTimeOffset(serviceBusMessage.ScheduledEnqueueTimeUtc, TimeSpan.Zero);
                    OnMessageReceived?.Invoke(this, new MessageReceivedArgs(
                        subscription,
                        enqueuedAt,
                        receivedAt,
                        processingTime
                    ));
                }
            }, (args) => OnServiceBusException?.Invoke(this, new ServiceBusExceptionArgs(subscription, args)));
        }

        private ISubscriptionClientWrapper GetClient(Subscription subscription)
        {
            if (clients.ContainsKey(subscription.ToString()))
            {
                throw new Exception("Connecting to the same subscription twice is not allowed.");
            }
            var client = subscriptionClientWrapperCreator.Create(subscription);
            clients[subscription.ToString()] = client;
            return client;
        }

        public void Dispose()
        {
            Task.WaitAll(clients.Select(x => x.Value.CloseAsync()).ToArray());
        }
    }
}
