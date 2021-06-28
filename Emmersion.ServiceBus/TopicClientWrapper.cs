using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    internal interface ITopicClientWrapper
    {
        Task SendAsync(Message message);
        Task ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc);
        Task CloseAsync();
    }

    internal class TopicClientWrapper : ITopicClientWrapper
    {
        private readonly TopicClient client;

        public TopicClientWrapper(string connectionString, string topicName)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"Invalid connectionString", nameof(connectionString));
            }
            if (string.IsNullOrEmpty(topicName))
            {
                throw new ArgumentException($"Invalid topicName", nameof(topicName));
            }
            client = new TopicClient(connectionString, topicName);
        }

        public Task CloseAsync()
        {
            return client.CloseAsync();
        }

        public Task SendAsync(Message message)
        {
            return client.SendAsync(message);
        }

        public Task ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            return client.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);
        }
    }
}
