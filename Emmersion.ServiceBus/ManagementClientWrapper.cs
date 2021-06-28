using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;

namespace Emmersion.ServiceBus
{
    internal interface IManagementClientWrapper
    {
        Task<bool> DoesTopicExistAsync(string topicName);
        Task<bool> DoesSubscriptionExistAsync(string topicName, string subscriptionName);
        Task CreateSubscriptionAsync(SubscriptionDescription description);
        Task CloseAsync();
    }

    internal class ManagementClientWrapper : IManagementClientWrapper
    {
        private ManagementClient client;

        public ManagementClientWrapper(string connectionString)
        {
            client = new ManagementClient(connectionString);
        }

        public Task<bool> DoesTopicExistAsync(string topicName)
        {
            return client.TopicExistsAsync(topicName);
        }

        public Task<bool> DoesSubscriptionExistAsync(string topicName, string subscriptionName)
        {
            return client.SubscriptionExistsAsync(topicName, subscriptionName);
        }

        public Task CreateSubscriptionAsync(SubscriptionDescription description)
        {
            return client.CreateSubscriptionAsync(description);
        }

        public Task CloseAsync()
        {
            return client.CloseAsync();
        }
    }
}
