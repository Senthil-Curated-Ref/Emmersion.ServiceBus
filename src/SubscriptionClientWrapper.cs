using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    internal interface ISubscriptionClientWrapper : IDisposable
    {
        void Subscribe(Action<string> messageHandler, Action<ExceptionReceivedEventArgs> exceptionHandler);
    }

    internal class SubscriptionClientWrapper : ISubscriptionClientWrapper
    {
        private readonly SubscriptionClient client;
        private readonly ISubscriptionConfig config;

        public SubscriptionClientWrapper(ISubscriptionConfig config)
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                throw new ArgumentException($"Invalid ConnectionString in ISubscriptionConfig", nameof(config.ConnectionString));
            }
            if (string.IsNullOrEmpty(config.TopicName))
            {
                throw new ArgumentException($"Invalid TopicName in ISubscriptionConfig", nameof(config.TopicName));
            }
            if (string.IsNullOrEmpty(config.SubscriptionName))
            {
                throw new ArgumentException($"Invalid SubscriptionName in ISubscriptionConfig", nameof(config.SubscriptionName));
            }
            if (config.MaxConcurrentMessages < 1)
            {
                throw new ArgumentException($"ISubscriptionConfig.MaxConcurrentMessages must be greater than zero.", nameof(config.MaxConcurrentMessages));
            }

            client = new SubscriptionClient(config.ConnectionString, config.TopicName, config.SubscriptionName);
            this.config = config;
        }

        public void Subscribe(Action<string> messageHandler, Action<ExceptionReceivedEventArgs> exceptionHandler)
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
                var serializedMessage = Encoding.UTF8.GetString(message.Body);
                messageHandler(serializedMessage);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await client.CompleteAsync(message.SystemProperties.LockToken);
                }
            }, options);
        }

        public void Dispose()
        {
            client.CloseAsync();
        }
    }
}