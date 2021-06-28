namespace Emmersion.ServiceBus
{
    public interface ISubscriptionConfig
    {
        string ConnectionString { get; }
        int MaxConcurrentMessages { get; }
        string SingleTopicConnectionString { get; }
        string SingleTopicName { get; }
        string SingleTopicSubscriptionName { get; }
        string EnvironmentFilter { get; }
    }
}