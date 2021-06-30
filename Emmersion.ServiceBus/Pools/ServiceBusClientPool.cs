using System;
using System.Threading.Tasks;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusClientPool : IAsyncDisposable
    {
        Task<IServiceBusClient> GetClientAsync(string connectionString);
    }

    internal class ServiceBusClientPool : IServiceBusClientPool
    {
        private readonly SemaphorePool<IServiceBusClient> pool = new SemaphorePool<IServiceBusClient>();

        public async Task<IServiceBusClient> GetClientAsync(string connectionString)
        {
            var result = await pool.Get(connectionString,
                () => Task.FromResult((IServiceBusClient) new ServiceBusClientWrapper(connectionString)));

            return result.Item;
        }

        public async ValueTask DisposeAsync()
        {
            await pool.Clear(async item => await item.DisposeAsync());
        }
    }
}