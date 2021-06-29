using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusProcessorPool : IAsyncDisposable
    {
        Task<IServiceBusProcessor> GetClientAsync(Subscription subscription);
        Task<IServiceBusProcessor> GetDeadLetterClientAsync(Subscription subscription);
        IServiceBusProcessor GetSingleTopicClientIfFirstTime();
    }

    internal class ServiceBusProcessorPool : IServiceBusProcessorPool
    {
        private Dictionary<string, IServiceBusProcessor> clients = new Dictionary<string, IServiceBusProcessor>();
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);
        private readonly IServiceBusProcessorCreator serviceBusProcessorCreator;
        private readonly ISubscriptionCreator subscriptionCreator;

        public ServiceBusProcessorPool(IServiceBusProcessorCreator serviceBusProcessorCreator,
            ISubscriptionCreator subscriptionCreator)
        {
            this.serviceBusProcessorCreator = serviceBusProcessorCreator;
            this.subscriptionCreator = subscriptionCreator;
        }

        public Task<IServiceBusProcessor> GetClientAsync(Subscription subscription)
        {
            return GetClient(subscription.ToString(), subscription, () => serviceBusProcessorCreator.Create(subscription));
        }

        public Task<IServiceBusProcessor> GetDeadLetterClientAsync(Subscription subscription)
        {
            return GetClient(subscription.ToString() + "-dead-letter", subscription, () => serviceBusProcessorCreator.CreateDeadLetter(subscription));
        }

        private async Task<IServiceBusProcessor> GetClient(string key, Subscription subscription, Func<IServiceBusProcessor> createClient)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (clients.ContainsKey(key))
                {
                    throw new Exception($"Connecting to the same subscription twice is not allowed: {subscription}");
                }
                await subscriptionCreator.CreateSubscriptionIfNecessaryAsync(subscription);
                var client = createClient();
                clients[key] = client;
                return client;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public IServiceBusProcessor GetSingleTopicClientIfFirstTime()
        {
            semaphoreSlim.Wait();
            try
            {
                if (!clients.ContainsKey("single-topic"))
                {
                    clients["single-topic"] = serviceBusProcessorCreator.CreateSingleTopic();
                    return clients["single-topic"];
                }
                return null;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.WhenAll(clients.Select(x => x.Value.CloseAsync()));
            clients.Clear();
        }
    }
}