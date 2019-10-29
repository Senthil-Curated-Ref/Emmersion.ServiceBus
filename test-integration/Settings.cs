using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace EL.ServiceBus.IntegrationTests
{
    public interface ISettings
    {
        ITopicConfig TopicConfig { get; }
        ISubscriptionConfig SubscriptionConfig { get; }
    }

    public class Settings : ISettings
    {
        static IConfiguration configuration;

        static Settings()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: false);
            configuration = configurationBuilder.Build();
        }

        public string ConnectionString => configuration.GetValue<string>("ConnectionStrings:ELServiceBus");
        public string TopicName => configuration.GetValue<string>("NameResolvers:ELServiceBusTopicName");
        public string SubscriptionName => configuration.GetValue<string>("NameResolvers:ELServiceBusSubscriberName");

        public ITopicConfig TopicConfig => new TestTopicConfig
        {
            ConnectionString = configuration.GetValue<string>("ConnectionString"),
            TopicName = configuration.GetValue<string>("TopicName")
        };

        public ISubscriptionConfig SubscriptionConfig => new TestSubscriptionConfig
        {
            ConnectionString = configuration.GetValue<string>("ConnectionString"),
            TopicName = configuration.GetValue<string>("TopicName"),
            SubscriptionName = configuration.GetValue<string>("SubscriptionName")
        };
    }
}