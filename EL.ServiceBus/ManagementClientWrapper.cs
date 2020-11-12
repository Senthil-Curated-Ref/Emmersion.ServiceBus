using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;

namespace EL.ServiceBus
{
    internal interface IManagementClientWrapper
    {
        Task<bool> DoesTopicExist(string topicName);
        Task<bool> DoesSubscriptionExist(string topicName, string subscriptionName);
        Task CreateSubscription(SubscriptionDescription description);
        Task CloseAsync();
    }

    internal class ManagementClientWrapper : IManagementClientWrapper
    {
        private ManagementClient client;

        public ManagementClientWrapper(string connectionString)
        {
            client = new ManagementClient(connectionString);
        }

        public Task<bool> DoesTopicExist(string topicName)
        {
            return client.TopicExistsAsync(topicName);
        }

        public Task<bool> DoesSubscriptionExist(string topicName, string subscriptionName)
        {
            return client.SubscriptionExistsAsync(topicName, subscriptionName);
        }

        public Task CreateSubscription(SubscriptionDescription description)
        {
            return client.CreateSubscriptionAsync(description);
        }

        public Task CloseAsync()
        {
            return client.CloseAsync();
        }
    }
}
