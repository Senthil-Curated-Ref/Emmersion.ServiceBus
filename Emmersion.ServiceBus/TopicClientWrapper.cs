using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus
{
    internal interface ITopicClientWrapper
    {
        Task SendAsync(ServiceBusMessage message);
        Task ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc);
        Task CloseAsync();
    }

    internal class TopicClientWrapper : ITopicClientWrapper
    {
        private readonly ServiceBusSender client;

        public TopicClientWrapper(ServiceBusSender client)
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
