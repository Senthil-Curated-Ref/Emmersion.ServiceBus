using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("EL.ServiceBus.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace EL.ServiceBus
{
    public class DependencyInjectionConfig
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            ConfigureSubscriberServices(services);
            ConfigurePublisherServices(services);
        }

        public static void ConfigureSubscriberServices(IServiceCollection services)
        {
            services.AddSingleton<IMessageSubscriber, MessageSubscriber>();
            services.AddTransient<ISubscriptionClientWrapper, SubscriptionClientWrapper>();
        }

        public static void ConfigurePublisherServices(IServiceCollection services)
        {
            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddTransient<ITopicClientWrapper, TopicClientWrapper>();
        }
    }
}
