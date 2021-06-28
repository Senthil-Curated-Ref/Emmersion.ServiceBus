namespace EL.ServiceBus
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

        private const string DeadLetterQueueSuffix = "/$DeadLetterQueue";

        public SubscriptionClientWrapperCreator(ISubscriptionConfig config)
        {
            this.config = config;
        }

        public ISubscriptionClientWrapper Create(Subscription subscription)
        {
            return new SubscriptionClientWrapper(config.ConnectionString, subscription.Topic.ToString(), subscription.SubscriptionName, config.MaxConcurrentMessages);
        }

        public ISubscriptionClientWrapper CreateDeadLetter(Subscription subscription)
        {
            return new SubscriptionClientWrapper(config.ConnectionString, subscription.Topic.ToString(), subscription.SubscriptionName + DeadLetterQueueSuffix, config.MaxConcurrentMessages);
        }

        public ISubscriptionClientWrapper CreateSingleTopic()
        {
            return new SubscriptionClientWrapper(config.SingleTopicConnectionString, config.SingleTopicName, config.SingleTopicSubscriptionName, config.MaxConcurrentMessages);
        }
    }
}
