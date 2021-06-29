using System.Runtime.CompilerServices;
using Emmersion.ServiceBus.Pools;
using Emmersion.ServiceBus.SdkWrappers;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Emmersion.ServiceBus.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Emmersion.ServiceBus
{
    public class DependencyInjectionConfig
    {
        public static void ConfigureSubscriberServices(IServiceCollection services)
        {
            services.AddSingleton<IMessageSubscriber, MessageSubscriber>();
            services.AddSingleton<IServiceBusProcessorPool, ServiceBusProcessorPool>();
            services.AddSingleton<IManagementClientWrapperPool, ManagementClientWrapperPool>();
            services.AddSingleton<IServiceBusClientPool, ServiceBusClientPool>();

            services.AddTransient<IServiceBusProcessor, ServiceBusProcessorWrapper>();
            services.AddTransient<IServiceBusProcessorCreator, ServiceBusProcessorCreator>();
            services.AddTransient<ISubscriptionCreator, SubscriptionCreator>();
            services.AddTransient<IMessageMapper, MessageMapper>();
            services.AddTransient<IMessageSerializer, MessageSerializer>();
        }

        public static void ConfigurePublisherServices(IServiceCollection services)
        {
            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<IServiceBusSenderPool, ServiceBusSenderPool>();
            services.AddSingleton<IServiceBusClientPool, ServiceBusClientPool>();

            services.AddTransient<IServiceBusSenderCreator, ServiceBusSenderCreator>();
            services.AddTransient<IMessageMapper, MessageMapper>();
            services.AddTransient<IMessageSerializer, MessageSerializer>();
        }
    }
}
