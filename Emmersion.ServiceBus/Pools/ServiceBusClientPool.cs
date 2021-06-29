using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emmersion.ServiceBus.SdkWrappers;

namespace Emmersion.ServiceBus.Pools
{
    internal interface IServiceBusClientPool : IAsyncDisposable
    {
        IServiceBusClient GetClient(string connectionString);
    }

    internal class ServiceBusClientPool : IServiceBusClientPool
    {
        private readonly Dictionary<string, IServiceBusClient> pool = new Dictionary<string, IServiceBusClient>();
        private static object threadLock = new object();
        
        public IServiceBusClient GetClient(string connectionString)
        {
            if (!pool.ContainsKey(connectionString))
            {
                lock (threadLock)
                {
                    if (!pool.ContainsKey(connectionString))
                    {
                        pool.Add(connectionString, new ServiceBusClientWrapper(connectionString));
                    }
                }
            }

            return pool[connectionString];
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var client in pool)
            {
                await client.Value.DisposeAsync();
            }
            pool.Clear();
        }
    }
}