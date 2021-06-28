namespace Emmersion.ServiceBus.IntegrationTests
{
    public class TestPublisherConfig : IPublisherConfig
    {
        public string ConnectionString { get; set; }

        public string SingleTopicConnectionString { get; set; }

        public string SingleTopicName { get; set; }
        
        public string Environment { get; set; }
    }
}