using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using Emmersion.ServiceBus.Pools;

namespace Emmersion.ServiceBus
{
    internal interface ISubscriptionCreator
    {
        Task CreateSubscriptionIfNecessaryAsync(Subscription subscription);
    }

    internal class SubscriptionCreator : ISubscriptionCreator
    {
        private IServiceBusAdministrationClientPool serviceBusAdministrationClientPool;
        
        public SubscriptionCreator(IServiceBusAdministrationClientPool serviceBusAdministrationClientPool)
        {
            this.serviceBusAdministrationClientPool = serviceBusAdministrationClientPool;
        }

        public async Task CreateSubscriptionIfNecessaryAsync(Subscription subscription)
        {   
            var client = serviceBusAdministrationClientPool.GetClient();
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

            var description = new CreateSubscriptionOptions(topicName, subscriptionName)
            {
                MaxDeliveryCount = 10,
                AutoDeleteOnIdle = subscriptionName.Contains("auto-delete") ? TimeSpan.FromMinutes(5) : TimeSpan.MaxValue,
                DefaultMessageTimeToLive = TimeSpan.FromDays(14),
                EnableDeadLetteringOnFilterEvaluationExceptions = true,
                DeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromSeconds(30)
            };
            await client.CreateSubscriptionAsync(description);
        }
    }
}
