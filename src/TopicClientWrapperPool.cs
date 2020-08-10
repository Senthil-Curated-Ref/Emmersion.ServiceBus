using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EL.ServiceBus
{
    internal interface ITopicClientWrapperPool : IDisposable
    {
        ITopicClientWrapper GetForTopic(string connectionString, string topicName);
    }

    internal class TopicClientWrapperPool : ITopicClientWrapperPool
    {
        private readonly Dictionary<string, ITopicClientWrapper> pool;
        private readonly ITopicClientWrapperCreator topicClientWrapperCreator;

        public TopicClientWrapperPool(ITopicClientWrapperCreator topicClientWrapperCreator)
        {
            pool = new Dictionary<string, ITopicClientWrapper>();
            this.topicClientWrapperCreator = topicClientWrapperCreator;
        }

        public void Dispose()
        {
            Task.WaitAll(pool.Select(x => x.Value.CloseAsync()).ToArray());
        }

        public ITopicClientWrapper GetForTopic(string connectionString, string topicName)
        {
            if (!pool.ContainsKey(topicName))
            {
                pool[topicName] = topicClientWrapperCreator.Create(connectionString, topicName);
            }
            return pool[topicName];
        }
    }
}
