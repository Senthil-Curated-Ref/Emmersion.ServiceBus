using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmersion.ServiceBus
{
    internal interface ITopicClientWrapperPool : IAsyncDisposable
    {
        ITopicClientWrapper GetForTopic(Topic topic);
        ITopicClientWrapper GetForSingleTopic();
    }

    internal class TopicClientWrapperPool : ITopicClientWrapperPool
    {
        private readonly Dictionary<string, ITopicClientWrapper> pool;
        private readonly IPublisherConfig publisherConfig;
        private readonly ITopicClientWrapperCreator topicClientWrapperCreator;
        private static object threadLock = new object();

        public TopicClientWrapperPool(IPublisherConfig publisherConfig, ITopicClientWrapperCreator topicClientWrapperCreator)
        {
            pool = new Dictionary<string, ITopicClientWrapper>();
            this.publisherConfig = publisherConfig;
            this.topicClientWrapperCreator = topicClientWrapperCreator;
        }
        
        public async ValueTask DisposeAsync()
        {
            await Task.WhenAll(pool.Select(x => x.Value.CloseAsync()));
            pool.Clear();
        }

        public ITopicClientWrapper GetForTopic(Topic topic)
        {
            return GetForTopic(publisherConfig.ConnectionString, topic.ToString());
        }

        public ITopicClientWrapper GetForSingleTopic()
        {
            return GetForTopic(publisherConfig.SingleTopicConnectionString, publisherConfig.SingleTopicName);
        }

        private ITopicClientWrapper GetForTopic(string connectionString, string topicName)
        {
            if (!pool.ContainsKey(topicName))
            {
                lock (threadLock)
                {
                    if (!pool.ContainsKey(topicName))
                    {
                        pool[topicName] = topicClientWrapperCreator.Create(connectionString, topicName);
                    }
                }
            }
            return pool[topicName];
        }
    }
}
