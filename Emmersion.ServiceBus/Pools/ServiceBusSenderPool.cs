using System;
using System.Threading.Tasks;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusSenderPool : IAsyncDisposable
    {
        Task<IServiceBusSender> GetForTopicAsync(Topic topic);
        Task<IServiceBusSender> GetForSingleTopicAsync();
    }

    internal class ServiceBusSenderPool : IServiceBusSenderPool
    {
        private readonly SemaphorePool<IServiceBusSender> pool = new SemaphorePool<IServiceBusSender>();
        private readonly IPublisherConfig publisherConfig;
        private readonly IServiceBusClientPool serviceBusClientPool;

        public ServiceBusSenderPool(IPublisherConfig publisherConfig, IServiceBusClientPool serviceBusClientPool)
        {
            this.publisherConfig = publisherConfig;
            this.serviceBusClientPool = serviceBusClientPool;
        }
        
        public async ValueTask DisposeAsync()
        {
            await pool.Clear(async item => await item.CloseAsync());
        }

        public async Task<IServiceBusSender> GetForTopicAsync(Topic topic)
        {
            return await GetForTopic(publisherConfig.ConnectionString, topic.ToString());
        }

        public async Task<IServiceBusSender> GetForSingleTopicAsync()
        {
            return await GetForTopic(publisherConfig.SingleTopicConnectionString, publisherConfig.SingleTopicName);
        }

        private async Task<IServiceBusSender> GetForTopic(string connectionString, string topicName)
        {
            var result = await pool.Get(topicName, () =>
            {
                var client = serviceBusClientPool.GetClient(connectionString);
                return Task.FromResult(client.CreateSender(topicName));
            });

            return result.Item;
        }
    }
}
