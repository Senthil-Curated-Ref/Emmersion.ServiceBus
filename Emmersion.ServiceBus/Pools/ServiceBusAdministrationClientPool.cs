using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusAdministrationClientPool
    {
        IServiceBusAdministrationClient GetClient();
    }

    internal class ServiceBusAdministrationClientPool : IServiceBusAdministrationClientPool
    {
        private readonly ISubscriptionConfig config;
        private IServiceBusAdministrationClient client;
        private static object threadLock = new object();

        public ServiceBusAdministrationClientPool(ISubscriptionConfig config)
        {
            this.config = config;
        }

        public IServiceBusAdministrationClient GetClient()
        {
            if (client != null)
            {
                return client;
            }

            lock (threadLock)
            {
                if (client == null)
                {
                    client = new ServiceBusAdministrationClient(config.ConnectionString);
                }
            }
            return client;
        }
    }
}
