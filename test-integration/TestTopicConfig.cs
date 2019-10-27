namespace EL.ServiceBus.IntegrationTests
{
    public class TestTopicConfig : ITopicConfig
    {
        public string ConnectionString { get; set; }

        public string TopicName { get; set; }
    }
}