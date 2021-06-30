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
        private readonly IServiceBusAdministrationClientFactory factory;
        private readonly SemaphorePool<IServiceBusAdministrationClient> pool = new SemaphorePool<IServiceBusAdministrationClient>();

        public ServiceBusAdministrationClientPool(ISubscriptionConfig config, IServiceBusAdministrationClientFactory factory)
        {
            this.config = config;
            this.factory = factory;
        }

        public async Task<IServiceBusAdministrationClient> GetClientAsync()
        {
            var result = await pool.Get(config.ConnectionString, () =>
                Task.FromResult(factory.Create(config.ConnectionString)));

            return result.Item;
        }
    }
}
