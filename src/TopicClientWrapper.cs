using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    internal interface ITopicClientWrapper
    {
        Task SendAsync(Message message);
        Task CloseAsync();
    }

    internal class TopicClientWrapper : ITopicClientWrapper
    {
        private readonly TopicClient client;

        public TopicClientWrapper(ITopicConfig config)
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                throw new ArgumentException($"Invalid ConnectionString in ITopicConfig", nameof(config.ConnectionString));
            }
            if (string.IsNullOrEmpty(config.TopicName))
            {
                throw new ArgumentException($"Invalid TopicName in ITopicConfig", nameof(config.TopicName));
            }
            client = new TopicClient(config.ConnectionString, config.TopicName);
        }

        public Task CloseAsync()
        {
            return client.CloseAsync();
        }

        public Task SendAsync(Message message)
        {
            return client.SendAsync(message);
        }
    }
}
