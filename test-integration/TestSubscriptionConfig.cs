namespace EL.ServiceBus.IntegrationTests
{
    public class TestSubscriptionConfig : ISubscriptionConfig
    {
        private readonly ISettings settings;

        public TestSubscriptionConfig(ISettings settings)
        {
            this.settings = settings;
        }
        public string ConnectionString => settings.ConnectionString;

        public string TopicName => settings.TopicName;

        public string SubscriptionName => settings.SubscriptionName;

        public int MaxConcurrentMessages => 1;
    }
}