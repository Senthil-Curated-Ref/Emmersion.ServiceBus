namespace Emmersion.ServiceBus.IntegrationTests
{
    public class TestSubscriptionConfig : ISubscriptionConfig
    {
        public string ConnectionString { get; set; }

        public int MaxConcurrentMessages => 1;

        public string SingleTopicConnectionString { get; set; }

        public string SingleTopicName { get; set; }

        public string SingleTopicSubscriptionName { get; set; }

        public string EnvironmentFilter => null;
    }
}