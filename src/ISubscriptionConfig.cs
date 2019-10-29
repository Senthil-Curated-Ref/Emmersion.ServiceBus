namespace EL.ServiceBus
{
    public interface ISubscriptionConfig
    {
        string ConnectionString { get; }
        string TopicName { get; }
        string SubscriptionName { get; }
        int MaxConcurrentMessages { get; }
    }
}