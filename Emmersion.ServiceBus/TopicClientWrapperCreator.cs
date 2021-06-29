using Emmersion.ServiceBus.Pools;

namespace Emmersion.ServiceBus
{
    internal interface ITopicClientWrapperCreator
    {
        ITopicClientWrapper Create(string connectionString, string topicName);
    }

    internal class TopicClientWrapperCreator : ITopicClientWrapperCreator
    {
        private readonly IServiceBusClientPool serviceBusClientPool;

        public TopicClientWrapperCreator(IServiceBusClientPool serviceBusClientPool)
        {
            this.serviceBusClientPool = serviceBusClientPool;
        }

        public ITopicClientWrapper Create(string connectionString, string topicName)
        {
            var serviceBusClient = serviceBusClientPool.GetClient(connectionString);
            return new TopicClientWrapper(serviceBusClient.CreateSender(topicName));
        }
    }
}
