using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus
{
    internal interface IManagementClientWrapperPool
    {
        IServiceBusAdministrationClient GetClient();
    }

    internal class ManagementClientWrapperPool : IManagementClientWrapperPool
    {
        private readonly ISubscriptionConfig config;
        private IServiceBusAdministrationClient client;
        private static object threadLock = new object();

        public ManagementClientWrapperPool(ISubscriptionConfig config)
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
