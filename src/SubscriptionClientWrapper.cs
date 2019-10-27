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
            client = new SubscriptionClient(config.ConnectionString, config.TopicName, config.SubscriptionName);
            this.config = config;
        }

        public void Subscribe(Action<string> messageHandler, Action<ExceptionReceivedEventArgs> exceptionHandler)
        {
            var options = new MessageHandlerOptions((ExceptionReceivedEventArgs exceptionReceivedEventArgs) => {
                exceptionHandler(exceptionReceivedEventArgs);
                return Task.CompletedTask;
            })
            {
                MaxConcurrentCalls = config.MaxConcurrentMessages,
                AutoComplete = false
            };
            client.RegisterMessageHandler(async (Message message, CancellationToken token) => {
                var serializedMessage = Encoding.UTF8.GetString(message.Body);
                messageHandler(serializedMessage);
                if (!token.IsCancellationRequested)
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