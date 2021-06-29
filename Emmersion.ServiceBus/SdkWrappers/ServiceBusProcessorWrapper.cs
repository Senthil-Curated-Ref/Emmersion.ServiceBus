using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus.SdkWrappers
{
    internal interface IServiceBusProcessor
    {
        Task RegisterMessageHandlerAsync(Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> exceptionHandler);
        Task CloseAsync();
    }

    internal class ServiceBusProcessorWrapper : IServiceBusProcessor
    {
        private readonly ServiceBusProcessor client;

        public ServiceBusProcessorWrapper(ServiceBusProcessor client)
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