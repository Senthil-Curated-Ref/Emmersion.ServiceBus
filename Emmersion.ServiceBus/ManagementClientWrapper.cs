using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;

namespace Emmersion.ServiceBus
{
    internal interface IManagementClientWrapper
    {
        Task<bool> DoesTopicExistAsync(string topicName);
        Task<bool> DoesSubscriptionExistAsync(string topicName, string subscriptionName);
        Task CreateSubscriptionAsync(CreateSubscriptionOptions description);
    }

    internal class ManagementClientWrapper : IManagementClientWrapper
    {
        private ServiceBusAdministrationClient client;

        public ManagementClientWrapper(string connectionString)
        {
            client = new ServiceBusAdministrationClient(connectionString);
        }

        public async Task<bool> DoesTopicExistAsync(string topicName)
        {
            var exists = await client.TopicExistsAsync(topicName);
            return exists.Value;
        }

        public async Task<bool> DoesSubscriptionExistAsync(string topicName, string subscriptionName)
        {
            var exists = await client.SubscriptionExistsAsync(topicName, subscriptionName);
            return exists.Value;
        }

        public Task CreateSubscriptionAsync(CreateSubscriptionOptions description)
        {
            return client.CreateSubscriptionAsync(description);
        }
    }
}
