using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Emmersion.ServiceBus.IntegrationTests
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
            configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<Settings>().Build();
        }

        public IPublisherConfig PublisherConfig => new TestPublisherConfig
        {
            ConnectionString = configuration.GetValue<string>("ServiceBus:ConnectionString"),
            SingleTopicConnectionString = configuration.GetValue<string>("ServiceBus:SingleTopicConnectionString"),
            SingleTopicName = configuration.GetValue<string>("ServiceBus:SingleTopicName"),
            Environment = "emmersion.servicebus.integration-tests"
        };

        public ISubscriptionConfig SubscriptionConfig => new TestSubscriptionConfig
        {
            ConnectionString = configuration.GetValue<string>("ServiceBus:ConnectionString"),
            SingleTopicConnectionString = configuration.GetValue<string>("ServiceBus:SingleTopicConnectionString"),
            SingleTopicName = configuration.GetValue<string>("ServiceBus:SingleTopicName"),
            SingleTopicSubscriptionName = configuration.GetValue<string>("ServiceBus:SingleTopicSubscriptionName")
        };
    }
}