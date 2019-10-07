using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace EL.ServiceBus.IntegrationTests
{
    public interface ISettings
    {
        string ConnectionString { get; }
        string TopicName { get; }
    }

    public class Settings : ISettings, INameResolver
    {
        static IConfiguration configuration;

        static Settings()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: false);
            configuration = configurationBuilder.Build();
        }

        public string Resolve(string name)
        {
            return configuration.GetValue<string>($"resolvers:{name}");
        }

        public string ConnectionString => configuration.GetValue<string>("ConnectionStrings:el-service-bus");
        public string TopicName => configuration.GetValue<string>("resolvers:el-service-bus-topic-name");
    }
}