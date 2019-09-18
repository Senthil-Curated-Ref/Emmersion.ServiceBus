using El.ServiceBus;

namespace EL.ServiceBus.IntegrationTests
{
    public class TestTopicConfig : ITopicConfig
    {
        private readonly ISettings settings;

        public TestTopicConfig(ISettings settings)
        {
            this.settings = settings;
        }

        public string ConnectionString => settings.ConnectionString;

        public string TopicName => settings.TopicName;
    }
}