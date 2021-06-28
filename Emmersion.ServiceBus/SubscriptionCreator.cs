using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;

namespace EL.ServiceBus
{
    internal interface ISubscriptionCreator
    {
        Task CreateSubscriptionIfNecessaryAsync(Subscription subscription);
    }

    internal class SubscriptionCreator : ISubscriptionCreator
    {
        private IManagementClientWrapperPool managementClientWrapperPool;
        
        public SubscriptionCreator(IManagementClientWrapperPool managementClientWrapperPool)
        {
            this.managementClientWrapperPool = managementClientWrapperPool;
        }

        public async Task CreateSubscriptionIfNecessaryAsync(Subscription subscription)
        {   
            var client = managementClientWrapperPool.GetClient();
            var topicName = subscription.Topic.ToString();
            var subscriptionName = subscription.SubscriptionName;
            var subscriptionExists = await client.DoesSubscriptionExistAsync(topicName, subscriptionName);
            if (subscriptionExists)
            {
                return;
            }

            var topicExists = await client.DoesTopicExistAsync(topicName);
            if (!topicExists)
            {
                throw new Exception($"Topic {topicName} does not exist");
            }

            var description = new SubscriptionDescription(topicName, subscriptionName)
            {
                MaxDeliveryCount = 10,
                AutoDeleteOnIdle = subscriptionName.Contains("auto-delete") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                DefaultMessageTimeToLive = TimeSpan.FromDays(14),
                EnableDeadLetteringOnFilterEvaluationExceptions = true,
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromSeconds(30)
            };
            await client.CreateSubscriptionAsync(description);
        }
    }
}
