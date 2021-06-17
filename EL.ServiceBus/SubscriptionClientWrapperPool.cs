using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    internal interface ISubscriptionClientWrapperPool : IDisposable
    {
        Task<ISubscriptionClientWrapper> GetClient(Subscription subscription);
        Task<ISubscriptionClientWrapper> GetDeadLetterClient(Subscription subscription);
        ISubscriptionClientWrapper GetSingleTopicClientIfFirstTime();
    }

    internal class SubscriptionClientWrapperPool : ISubscriptionClientWrapperPool
    {
        private Dictionary<string, ISubscriptionClientWrapper> clients = new Dictionary<string, ISubscriptionClientWrapper>();
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);
        private readonly ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator;
        private readonly ISubscriptionCreator subscriptionCreator;

        public SubscriptionClientWrapperPool(ISubscriptionClientWrapperCreator subscriptionClientWrapperCreator,
            ISubscriptionCreator subscriptionCreator)
        {
            this.subscriptionClientWrapperCreator = subscriptionClientWrapperCreator;
            this.subscriptionCreator = subscriptionCreator;
        }

        public Task<ISubscriptionClientWrapper> GetClient(Subscription subscription)
        {
            return GetClient(subscription.ToString(), subscription, () => subscriptionClientWrapperCreator.Create(subscription));
        }

        public Task<ISubscriptionClientWrapper> GetDeadLetterClient(Subscription subscription)
        {
            return GetClient(subscription.ToString() + "-dead-letter", subscription, () => subscriptionClientWrapperCreator.CreateDeadLetter(subscription));
        }

        private async Task<ISubscriptionClientWrapper> GetClient(string key, Subscription subscription, Func<ISubscriptionClientWrapper> createClient)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (clients.ContainsKey(key))
                {
                    throw new Exception($"Connecting to the same subscription twice is not allowed: {subscription}");
                }
                await subscriptionCreator.CreateSubscriptionIfNecessary(subscription);
                var client = createClient();
                clients[key] = client;
                return client;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public ISubscriptionClientWrapper GetSingleTopicClientIfFirstTime()
        {
            semaphoreSlim.Wait();
            try
            {
                if (!clients.ContainsKey("single-topic"))
                {
                    clients["single-topic"] = subscriptionClientWrapperCreator.CreateSingleTopic();
                    return clients["single-topic"];
                }
                return null;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void Dispose()
        {
            Task.WaitAll(clients.Select(x => x.Value.CloseAsync()).ToArray());
        }
    }
}