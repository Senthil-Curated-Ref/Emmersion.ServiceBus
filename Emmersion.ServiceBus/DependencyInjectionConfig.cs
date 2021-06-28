using System.Runtime.CompilerServices;
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
            services.AddSingleton<ISubscriptionClientWrapperPool, SubscriptionClientWrapperPool>();
            services.AddSingleton<IManagementClientWrapperPool, ManagementClientWrapperPool>();

            services.AddTransient<ISubscriptionClientWrapper, SubscriptionClientWrapper>();
            services.AddTransient<ISubscriptionClientWrapperCreator, SubscriptionClientWrapperCreator>();
            services.AddTransient<ISubscriptionCreator, SubscriptionCreator>();
            services.AddTransient<IMessageMapper, MessageMapper>();
            services.AddTransient<IMessageSerializer, MessageSerializer>();
        }

        public static void ConfigurePublisherServices(IServiceCollection services)
        {
            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<ITopicClientWrapperPool, TopicClientWrapperPool>();

            services.AddTransient<ITopicClientWrapperCreator, TopicClientWrapperCreator>();
            services.AddTransient<IMessageMapper, MessageMapper>();
            services.AddTransient<IMessageSerializer, MessageSerializer>();
        }
    }
}
