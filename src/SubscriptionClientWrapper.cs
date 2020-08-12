using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    internal interface ISubscriptionClientWrapper
    {
        void RegisterMessageHandler(Action<Microsoft.Azure.ServiceBus.Message> messageHandler, Action<ExceptionReceivedEventArgs> exceptionHandler);
        Task CloseAsync();
    }

    internal class SubscriptionClientWrapper : ISubscriptionClientWrapper
    {
        private readonly SubscriptionClient client;
        private readonly ISubscriptionConfig config;

        public SubscriptionClientWrapper(ISubscriptionConfig config, Subscription subscription)
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                throw new ArgumentException($"Invalid ConnectionString in ISubscriptionConfig", nameof(config.ConnectionString));
            }
            if (subscription == null)
            {
                throw new ArgumentException($"Invalid Subscription", nameof(subscription));
            }
            if (config.MaxConcurrentMessages < 1)
            {
                throw new ArgumentException($"ISubscriptionConfig.MaxConcurrentMessages must be greater than zero.", nameof(config.MaxConcurrentMessages));
            }

            client = new SubscriptionClient(config.ConnectionString, subscription.Topic.ToString(), subscription.SubscriptionName);
            this.config = config;
        }

        public void RegisterMessageHandler(Action<Microsoft.Azure.ServiceBus.Message> messageHandler, Action<ExceptionReceivedEventArgs> exceptionHandler)
        {
            var options = new MessageHandlerOptions(exceptionReceivedEventArgs => {
                exceptionHandler(exceptionReceivedEventArgs);
                return Task.CompletedTask;
            })
            {
                MaxConcurrentCalls = config.MaxConcurrentMessages,
                AutoComplete = false
            };
            client.RegisterMessageHandler(async (message, cancellationToken) => {
                messageHandler(message);
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