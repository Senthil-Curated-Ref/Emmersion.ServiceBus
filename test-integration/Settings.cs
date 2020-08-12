using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace EL.ServiceBus.IntegrationTests
{
    public interface ISettings
    {
        IPublisherConfig PublisherConfig { get; }
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

        public IPublisherConfig PublisherConfig => new TestPublisherConfig
        {
            ConnectionString = configuration.GetValue<string>("ConnectionString"),
            SingleTopicConnectionString = configuration.GetValue<string>("SingleTopicConnectionString"),
            SingleTopicName = configuration.GetValue<string>("SingleTopicName")
        };

        public ISubscriptionConfig SubscriptionConfig => new TestSubscriptionConfig
        {
            ConnectionString = configuration.GetValue<string>("ConnectionString")
        };
    }
}