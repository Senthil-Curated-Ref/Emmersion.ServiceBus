namespace EL.ServiceBus.IntegrationTests
{
    public class TestSubscriptionConfig : ISubscriptionConfig
    {
        public string ConnectionString { get; set; }

        public string TopicName { get; set; }

        public string SubscriptionName { get; set; }

        public int MaxConcurrentMessages => 1;
    }
}