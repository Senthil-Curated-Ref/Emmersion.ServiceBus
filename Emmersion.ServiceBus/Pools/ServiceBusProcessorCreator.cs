using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusProcessorCreator
    {
        IServiceBusProcessor Create(Subscription subscription);
        IServiceBusProcessor CreateDeadLetter(Subscription subscription);
        IServiceBusProcessor CreateSingleTopic();
    }

    internal class ServiceBusProcessorCreator : IServiceBusProcessorCreator
    {
        private readonly ISubscriptionConfig config;
        private readonly IServiceBusClientPool serviceBusClientPool;

        private const string DeadLetterQueueSuffix = "/$DeadLetterQueue";

        public ServiceBusProcessorCreator(ISubscriptionConfig config, IServiceBusClientPool serviceBusClientPool)
        {
            this.config = config;
            this.serviceBusClientPool = serviceBusClientPool;
        }

        public IServiceBusProcessor Create(Subscription subscription)
        {
            var serviceBusClient = serviceBusClientPool.GetClient(config.ConnectionString);
            return serviceBusClient.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName, config.MaxConcurrentMessages);
        }

        public IServiceBusProcessor CreateDeadLetter(Subscription subscription)
        {
            var serviceBusClient = serviceBusClientPool.GetClient(config.ConnectionString);
            return serviceBusClient.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName + DeadLetterQueueSuffix, config.MaxConcurrentMessages);
        }

        public IServiceBusProcessor CreateSingleTopic()
        {
            var serviceBusClient = serviceBusClientPool.GetClient(config.SingleTopicConnectionString);
            return serviceBusClient.CreateProcessor(config.SingleTopicName, config.SingleTopicSubscriptionName, config.MaxConcurrentMessages);
        }
    }
}
