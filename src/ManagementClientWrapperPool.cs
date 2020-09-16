using System;

namespace EL.ServiceBus
{
    internal interface IManagementClientWrapperPool : IDisposable
    {
        IManagementClientWrapper GetClient();
    }

    internal class ManagementClientWrapperPool : IManagementClientWrapperPool
    {
        private readonly ISubscriptionConfig config;
        private ManagementClientWrapper client;
        private static object threadLock = new object();

        public ManagementClientWrapperPool(ISubscriptionConfig config)
        {
            this.config = config;
        }

        public IManagementClientWrapper GetClient()
        {
            if (client != null)
            {
                return client;
            }

            lock (threadLock)
            {
                if (client == null)
                {
                    client = new ManagementClientWrapper(config.ConnectionString);
                }
            }
            return client;
        }

        public void Dispose()
        {
            client?.CloseAsync().Wait();
        }
    }
}
