namespace EL.ServiceBus
{
    internal interface ISubscriptionClientWrapperCreator
    {
        ISubscriptionClientWrapper Create(Subscription subscription);
    }

    internal class SubscriptionClientWrapperCreator : ISubscriptionClientWrapperCreator
    {
        private readonly ISubscriptionConfig config;

        public SubscriptionClientWrapperCreator(ISubscriptionConfig config)
        {
            this.config = config;
        }

        public ISubscriptionClientWrapper Create(Subscription subscription)
        {
            return new SubscriptionClientWrapper(config, subscription);
        }
    }
}
