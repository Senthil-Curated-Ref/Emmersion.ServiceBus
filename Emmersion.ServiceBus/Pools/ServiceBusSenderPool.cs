using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusSenderPool : IAsyncDisposable
    {
        IServiceBusSender GetForTopic(Topic topic);
        IServiceBusSender GetForSingleTopic();
    }

    internal class ServiceBusSenderPool : IServiceBusSenderPool
    {
        private readonly Dictionary<string, IServiceBusSender> pool;
        private readonly IPublisherConfig publisherConfig;
        private readonly IServiceBusClientPool serviceBusClientPool;
        private static object threadLock = new object();

        public ServiceBusSenderPool(IPublisherConfig publisherConfig, IServiceBusClientPool serviceBusClientPool)
        {
            pool = new Dictionary<string, IServiceBusSender>();
            this.publisherConfig = publisherConfig;
            this.serviceBusClientPool = serviceBusClientPool;
        }
        
        public async ValueTask DisposeAsync()
        {
            await Task.WhenAll(pool.Select(x => x.Value.CloseAsync()));
            pool.Clear();
        }

        public IServiceBusSender GetForTopic(Topic topic)
        {
            return GetForTopic(publisherConfig.ConnectionString, topic.ToString());
        }

        public IServiceBusSender GetForSingleTopic()
        {
            return GetForTopic(publisherConfig.SingleTopicConnectionString, publisherConfig.SingleTopicName);
        }

        private IServiceBusSender GetForTopic(string connectionString, string topicName)
        {
            if (!pool.ContainsKey(topicName))
            {
                lock (threadLock)
                {
                    if (!pool.ContainsKey(topicName))
                    {
                        var client = serviceBusClientPool.GetClient(connectionString);
                        pool[topicName] = client.CreateSender(topicName);
                    }
                }
            }
            return pool[topicName];
        }
    }
}
