using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emmersion.ServiceBus.Pools
{
    internal class SemaphorePool<T>
    {
        private readonly Dictionary<string, T> pool = new Dictionary<string, T>();
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);
        
        public async Task<SemaphorePoolResult<T>> Get(string key, Func<Task<T>> creator)
        {
            if (!pool.ContainsKey(key))
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    if (!pool.ContainsKey(key))
                    {
                        pool[key] = await creator();
                        return new SemaphorePoolResult<T>(pool[key], true);
                    }
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
            
            return new SemaphorePoolResult<T>(pool[key], false);
        }

        public async Task Clear(Func<T, Task> disposeTask)
        {
            foreach (var item in pool)
            {
                await disposeTask(item.Value);
            }
            pool.Clear();
        }
    }

    internal class SemaphorePoolResult<T>
    {
        public T Item { get; }
        public bool NewlyCreated { get; }

        public SemaphorePoolResult(T item, bool newlyCreated)
        {
            Item = item;
            NewlyCreated = newlyCreated;
        }
    }
}