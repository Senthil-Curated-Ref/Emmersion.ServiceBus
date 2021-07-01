namespace Emmersion.ServiceBus.SdkWrappers
{
    internal interface IServiceBusAdministrationClientFactory
    {
        IServiceBusAdministrationClient Create(string connectionString);
    }

    internal class ServiceBusAdministrationClientFactory : IServiceBusAdministrationClientFactory
    {
        public IServiceBusAdministrationClient Create(string connectionString)
        {
            return new ServiceBusAdministrationClientWrapper(connectionString);
        }
    }
}