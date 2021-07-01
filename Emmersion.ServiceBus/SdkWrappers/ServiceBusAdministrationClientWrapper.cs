using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus.Administration;

namespace Emmersion.ServiceBus.SdkWrappers
{
    internal interface IServiceBusAdministrationClient
    {
        Task<bool> DoesTopicExistAsync(string topicName);
        Task<bool> DoesSubscriptionExistAsync(string topicName, string subscriptionName);
        Task CreateSubscriptionAsync(CreateSubscriptionOptions description);
    }

    internal class ServiceBusAdministrationClientWrapper : IServiceBusAdministrationClient
    {
        private ServiceBusAdministrationClient client;

        public ServiceBusAdministrationClientWrapper(string connectionString)
        {
            client = new ServiceBusAdministrationClient(connectionString);
        }

        public async Task<bool> DoesTopicExistAsync(string topicName)
        {
            return (await client.TopicExistsAsync(topicName)).Value;
        }

        public async Task<bool> DoesSubscriptionExistAsync(string topicName, string subscriptionName)
        {
            return (await client.SubscriptionExistsAsync(topicName, subscriptionName)).Value;
        }

        public Task CreateSubscriptionAsync(CreateSubscriptionOptions description)
        {
            return client.CreateSubscriptionAsync(description);
        }
    }
}
