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
        Task<IServiceBusProcessor> GetProcessorAsync(Subscription subscription);
        Task<IServiceBusProcessor> GetDeadLetterProcessorAsync(Subscription subscription);
        IServiceBusProcessor GetSingleTopicProcessorIfFirstTime();
    }

    internal class ServiceBusProcessorPool : IServiceBusProcessorPool
    {
        private Dictionary<string, IServiceBusProcessor> processors = new Dictionary<string, IServiceBusProcessor>();
        static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);
        private readonly ISubscriptionCreator subscriptionCreator;
        private readonly IServiceBusClientPool serviceBusClientPool;
        private readonly ISubscriptionConfig subscriptionConfig;
        
        private const string DeadLetterQueueSuffix = "/$DeadLetterQueue";

        public ServiceBusProcessorPool(ISubscriptionCreator subscriptionCreator,
            IServiceBusClientPool serviceBusClientPool,
            ISubscriptionConfig subscriptionConfig)
        {
            this.subscriptionCreator = subscriptionCreator;
            this.serviceBusClientPool = serviceBusClientPool;
            this.subscriptionConfig = subscriptionConfig;
        }

        public Task<IServiceBusProcessor> GetProcessorAsync(Subscription subscription)
        {
            return GetClient(subscription.ToString(), subscription, "");
        }

        public Task<IServiceBusProcessor> GetDeadLetterProcessorAsync(Subscription subscription)
        {
            return GetClient(subscription + "-dead-letter", subscription, DeadLetterQueueSuffix);
        }

        private async Task<IServiceBusProcessor> GetClient(string key, Subscription subscription, string suffix)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (processors.ContainsKey(key))
                {
                    throw new Exception($"Connecting to the same subscription twice is not allowed: {subscription}");
                }
                await subscriptionCreator.CreateSubscriptionIfNecessaryAsync(subscription);
                var client = serviceBusClientPool.GetClient(subscriptionConfig.ConnectionString);
                var processor = client.CreateProcessor(
                    subscription.Topic.ToString(),
                    subscription.SubscriptionName + suffix,
                    subscriptionConfig.MaxConcurrentMessages);
                processors[key] = processor;
                return processor;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public IServiceBusProcessor GetSingleTopicProcessorIfFirstTime()
        {
            semaphoreSlim.Wait();
            try
            {
                if (!processors.ContainsKey("single-topic"))
                {
                    var client = serviceBusClientPool.GetClient(subscriptionConfig.SingleTopicConnectionString);
                    processors["single-topic"] = client.CreateProcessor(
                        subscriptionConfig.SingleTopicName,
                        subscriptionConfig.SingleTopicSubscriptionName,
                        subscriptionConfig.MaxConcurrentMessages);
                    return processors["single-topic"];
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
            await Task.WhenAll(processors.Select(x => x.Value.CloseAsync()));
            processors.Clear();
        }
    }
}