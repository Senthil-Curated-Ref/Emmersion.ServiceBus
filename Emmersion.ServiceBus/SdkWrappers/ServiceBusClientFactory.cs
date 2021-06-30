namespace Emmersion.ServiceBus.SdkWrappers
{
    internal interface IServiceBusClientFactory
    {
        IServiceBusClient Create(string connectionString);
    }

    internal class ServiceBusClientFactory : IServiceBusClientFactory
    {
        public IServiceBusClient Create(string connectionString)
        {
            return new ServiceBusClientWrapper(connectionString);
        }
    }
}