using System;
using System.Threading.Tasks;

namespace Emmersion.ServiceBus
{
    internal interface IManagementClientWrapperPool : IAsyncDisposable
    {
        IManagementClientWrapper GetClient();
    }

    internal class ManagementClientWrapperPool : IManagementClientWrapperPool
    {
        private readonly ISubscriptionConfig config;
        private IManagementClientWrapper client;
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

        public async ValueTask DisposeAsync()
        {
            if (client != null)
            {
                await client.CloseAsync();
                client = null;
            }
        }
    }
}