using System;
using System.Threading.Tasks;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusProcessorPool : IAsyncDisposable
    {
        Task<IServiceBusProcessor> GetProcessorAsync(Subscription subscription);
        Task<IServiceBusProcessor> GetDeadLetterProcessorAsync(Subscription subscription);
        Task<IServiceBusProcessor> GetSingleTopicProcessorIfFirstTime();
    }

    internal class ServiceBusProcessorPool : IServiceBusProcessorPool
    {
        private SemaphorePool<IServiceBusProcessor> processors = new SemaphorePool<IServiceBusProcessor>();
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
            var processor = await processors.Get(key, async () =>
            {
                await subscriptionCreator.CreateSubscriptionIfNecessaryAsync(subscription);
                var client = await serviceBusClientPool.GetClientAsync(subscriptionConfig.ConnectionString);
                return client.CreateProcessor(
                    subscription.Topic.ToString(),
                    subscription.SubscriptionName + suffix,
                    subscriptionConfig.MaxConcurrentMessages);
            });

            if (!processor.NewlyCreated)
            {
                throw new Exception($"Connecting to the same subscription twice is not allowed: {subscription}");
            }

            return processor.Item;
        }

        public async Task<IServiceBusProcessor> GetSingleTopicProcessorIfFirstTime()
        {
            var processor = await processors.Get("single-topic", async () =>
            {
                var client = await serviceBusClientPool.GetClientAsync(subscriptionConfig.SingleTopicConnectionString);
                return client.CreateProcessor(
                    subscriptionConfig.SingleTopicName,
                    subscriptionConfig.SingleTopicSubscriptionName,
                    subscriptionConfig.MaxConcurrentMessages);
            });

            if (processor.NewlyCreated)
            {
                return processor.Item;
            }

            return null;
        }

        public async ValueTask DisposeAsync()
        {
            await processors.Clear(async processor => await processor.CloseAsync());
        }
    }
}