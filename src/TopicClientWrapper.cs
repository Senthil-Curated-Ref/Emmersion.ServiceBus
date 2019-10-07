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
