using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    internal interface ISubscriptionClientWrapperPool : IDisposable
    {
        ISubscriptionClientWrapper GetClient(Subscription subscription);
        ISubscriptionClientWrapper GetDeadLetterClient(Subscription subscription);
        ISubscriptionClientWrapper GetSingleTopicClientIfFirstTime();
    }

    internal class SubscriptionClientWrapperPool : ISubscriptionClientWrapperPool
    {
        private Dictionary<string, ISubscriptionClientWrapper> clients = new Dictionary<string, ISubscriptionClientWrapper>();
        private static object threadLock = new object();
        private readonly ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator;
        private readonly ISubscriptionCreator subscriptionCreator;

        public SubscriptionClientWrapperPool(ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator,
            ISubscriptionCreator subscriptionCreator)
        {
            this.subscriptionClientWrapperCreator = subscriptionClientWrapperCreator;
            this.subscriptionCreator = subscriptionCreator;
        }

        public ISubscriptionClientWrapper GetClient(Subscription subscription)
        {
            return GetClient(subscription.ToString(), subscription, () => subscriptionClientWrapperCreator.Create(subscription));
        }

        public ISubscriptionClientWrapper GetDeadLetterClient(Subscription subscription)
        {
            return GetClient(subscription.ToString() + "-dead-letter", subscription, () => subscriptionClientWrapperCreator.CreateDeadLetter(subscription));
        }

        private ISubscriptionClientWrapper GetClient(string key, Subscription subscription, Func<ISubscriptionClientWrapper> createAction)
        {
            lock (threadLock)
            {
                if (clients.ContainsKey(key))
                {
                    throw new Exception($"Connecting to the same subscription twice is not allowed: {subscription}");
                }
                subscriptionCreator.CreateSubscriptionIfNecessary(subscription).Wait();
                var client = createAction();
                clients[key] = client;
                return client;
            }
        }

        public ISubscriptionClientWrapper GetSingleTopicClientIfFirstTime()
        {
            lock (threadLock)
            {
                if (!clients.ContainsKey("single-topic"))
                {
                    clients["single-topic"] = subscriptionClientWrapperCreator.CreateSingleTopic();
                    return clients["single-topic"];
                }
                return null;
            }
        }

        public void Dispose()
        {
            Task.WaitAll(clients.Select(x => x.Value.CloseAsync()).ToArray());
        }
    }
}