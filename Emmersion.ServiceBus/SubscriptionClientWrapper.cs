using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus
{
    internal interface ISubscriptionClientWrapper
    {
        Task RegisterMessageHandlerAsync(Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> exceptionHandler);
        Task CloseAsync();
    }

    internal class SubscriptionClientWrapper : ISubscriptionClientWrapper
    {
        private readonly ServiceBusProcessor client;

        public SubscriptionClientWrapper(ServiceBusProcessor client)
        {
            this.client = client;
        }

        public async Task RegisterMessageHandlerAsync(Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> exceptionHandler)
        {
            client.ProcessMessageAsync += messageHandler;
            client.ProcessErrorAsync += exceptionHandler;
            await client.StartProcessingAsync();
        }

        public Task CloseAsync()
        {
            return client.CloseAsync();
        }
    }
}