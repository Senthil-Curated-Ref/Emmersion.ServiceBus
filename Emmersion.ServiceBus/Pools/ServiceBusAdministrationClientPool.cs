using System.Threading.Tasks;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusAdministrationClientPool
    {
        Task<IServiceBusAdministrationClient> GetClientAsync();
    }

    internal class ServiceBusAdministrationClientPool : IServiceBusAdministrationClientPool
    {
        private readonly ISubscriptionConfig config;
        private SemaphorePool<IServiceBusAdministrationClient> pool = new SemaphorePool<IServiceBusAdministrationClient>();

        public ServiceBusAdministrationClientPool(ISubscriptionConfig config)
        {
            this.config = config;
        }

        public async Task<IServiceBusAdministrationClient> GetClientAsync()
        {
            var result = await pool.Get(config.ConnectionString, () =>
                Task.FromResult((IServiceBusAdministrationClient) new ServiceBusAdministrationClient(config.ConnectionString)));

            return result.Item;
        }
    }
}
