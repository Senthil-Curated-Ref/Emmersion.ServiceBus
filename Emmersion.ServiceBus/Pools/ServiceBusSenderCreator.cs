using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusSenderCreator
    {
        IServiceBusSender Create(string connectionString, string topicName);
    }

    internal class ServiceBusSenderCreator : IServiceBusSenderCreator
    {
        private readonly IServiceBusClientPool serviceBusClientPool;

        public ServiceBusSenderCreator(IServiceBusClientPool serviceBusClientPool)
        {
            this.serviceBusClientPool = serviceBusClientPool;
        }

        public IServiceBusSender Create(string connectionString, string topicName)
        {
            var serviceBusClient = serviceBusClientPool.GetClient(connectionString);
            return serviceBusClient.CreateSender(topicName);
        }
    }
}
