using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus.SdkWrappers
{
    internal interface IServiceBusSender
    {
        Task SendAsync(ServiceBusMessage message);
        Task ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc);
        Task CloseAsync();
    }

    internal class ServiceBusSenderWrapper : IServiceBusSender
    {
        private readonly ServiceBusSender client;

        public ServiceBusSenderWrapper(ServiceBusSender client)
        {
            this.client = client;
        }

        public Task CloseAsync()
        {
            return client.CloseAsync();
        }

        public Task SendAsync(ServiceBusMessage message)
        {
            return client.SendMessageAsync(message);
        }

        public Task ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc)
        {
            return client.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);
        }
    }
}
