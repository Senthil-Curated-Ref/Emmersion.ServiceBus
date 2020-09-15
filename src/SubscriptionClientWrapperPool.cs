using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    internal interface ISubscriptionClientWrapperPool : IDisposable
    {
        ISubscriptionClientWrapper GetClient(Subscription subscription);
        ISubscriptionClientWrapper GetSingleTopicClientIfFirstTime();
    }

    internal class SubscriptionClientWrapperPool : ISubscriptionClientWrapperPool
    {
        private Dictionary<string, ISubscriptionClientWrapper> clients = new Dictionary<string, ISubscriptionClientWrapper>();
        private static object threadLock = new object();
        private readonly ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator;

        public SubscriptionClientWrapperPool(ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator)
        {
            this.subscriptionClientWrapperCreator = subscriptionClientWrapperCreator;
        }

        public ISubscriptionClientWrapper GetClient(Subscription subscription)
        {
            lock (threadLock)
            {
                if (clients.ContainsKey(subscription.ToString()))
                {
                    throw new Exception($"Connecting to the same subscription twice is not allowed: {subscription}");
                }
                var client = subscriptionClientWrapperCreator.Create(subscription);
                clients[subscription.ToString()] = client;
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