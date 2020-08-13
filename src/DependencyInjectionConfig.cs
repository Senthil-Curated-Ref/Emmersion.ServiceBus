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
            services.AddTransient<ISubscriptionClientWrapperCreator, SubscriptionClientWrapperCreator>();
            services.AddTransient<IMessageMapper, MessageMapper>();
            services.AddTransient<IMessageSerializer, MessageSerializer>();
        }

        public static void ConfigurePublisherServices(IServiceCollection services)
        {
            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddTransient<ITopicClientWrapperPool, TopicClientWrapperPool>();
            services.AddTransient<ITopicClientWrapperCreator, TopicClientWrapperCreator>();
            services.AddTransient<IMessageMapper, MessageMapper>();
            services.AddTransient<IMessageSerializer, MessageSerializer>();
        }
    }
}
