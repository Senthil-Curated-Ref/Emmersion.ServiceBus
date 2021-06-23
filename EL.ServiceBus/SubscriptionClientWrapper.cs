using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    internal interface ISubscriptionClientWrapper
    {
        void RegisterMessageHandler(Func<Microsoft.Azure.ServiceBus.Message, Task> messageHandler, Func<ExceptionReceivedEventArgs, Task> exceptionHandler);
        Task CloseAsync();
    }

    internal class SubscriptionClientWrapper : ISubscriptionClientWrapper
    {
        private readonly SubscriptionClient client;
        private readonly int maxConcurrentMessages;

        public SubscriptionClientWrapper(string connectionString, string topicName, string subscriptionName, int maxConcurrentMessages)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"Invalid connectionString", nameof(connectionString));
            }
            if (string.IsNullOrEmpty(topicName))
            {
                throw new ArgumentException($"Invalid topic", nameof(topicName));
            }
            if (string.IsNullOrEmpty(subscriptionName))
            {
                throw new ArgumentException($"Invalid subscription", nameof(subscriptionName));
            }
            if (maxConcurrentMessages < 1)
            {
                throw new ArgumentException($"MaxConcurrentMessages must be greater than zero.", nameof(maxConcurrentMessages));
            }

            this.maxConcurrentMessages = maxConcurrentMessages;
            client = new SubscriptionClient(connectionString, topicName, subscriptionName);
        }

        public void RegisterMessageHandler(Func<Microsoft.Azure.ServiceBus.Message, Task> messageHandler, Func<ExceptionReceivedEventArgs, Task> exceptionHandler)
        {
            var options = new MessageHandlerOptions(exceptionHandler)
            {
                MaxConcurrentCalls = maxConcurrentMessages,
                AutoComplete = false
            };
            client.RegisterMessageHandler(async (message, cancellationToken) => {
                await messageHandler(message);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await client.CompleteAsync(message.SystemProperties.LockToken);
                }
            }, options);
        }

        public Task CloseAsync()
        {
            return client.CloseAsync();
        }
    }
}