namespace EL.ServiceBus
{
    public interface ISubscriptionConfig
    {
        string ConnectionString { get; }
        int MaxConcurrentMessages { get; }
    }
}