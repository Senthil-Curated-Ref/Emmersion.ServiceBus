using Emmersion.ServiceBus.Pools;

namespace Emmersion.ServiceBus
{
    internal interface ISubscriptionClientWrapperCreator
    {
        ISubscriptionClientWrapper Create(Subscription subscription);
        ISubscriptionClientWrapper CreateDeadLetter(Subscription subscription);
        ISubscriptionClientWrapper CreateSingleTopic();
    }

    internal class SubscriptionClientWrapperCreator : ISubscriptionClientWrapperCreator
    {
        private readonly ISubscriptionConfig config;
        private readonly IServiceBusClientPool serviceBusClientPool;

        private const string DeadLetterQueueSuffix = "/$DeadLetterQueue";

        public SubscriptionClientWrapperCreator(ISubscriptionConfig config, IServiceBusClientPool serviceBusClientPool)
        {
            this.config = config;
            this.serviceBusClientPool = serviceBusClientPool;
        }

        public ISubscriptionClientWrapper Create(Subscription subscription)
        {
            var serviceBusClient = serviceBusClientPool.GetClient(config.ConnectionString);
            return new SubscriptionClientWrapper(serviceBusClient.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName, config.MaxConcurrentMessages));
        }

        public ISubscriptionClientWrapper CreateDeadLetter(Subscription subscription)
        {
            var serviceBusClient = serviceBusClientPool.GetClient(config.ConnectionString);
            return new SubscriptionClientWrapper(serviceBusClient.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName + DeadLetterQueueSuffix, config.MaxConcurrentMessages));
        }

        public ISubscriptionClientWrapper CreateSingleTopic()
        {
            var serviceBusClient = serviceBusClientPool.GetClient(config.SingleTopicConnectionString);
            return new SubscriptionClientWrapper(serviceBusClient.CreateProcessor(config.SingleTopicName, config.SingleTopicSubscriptionName, config.MaxConcurrentMessages));
        }
    }
}
