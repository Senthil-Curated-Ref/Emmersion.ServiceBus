namespace Emmersion.ServiceBus
{
    public interface IPublisherConfig
    {
        string ConnectionString { get; }
        string SingleTopicConnectionString { get; }
        string SingleTopicName { get; }
        string Environment { get; }
    }
}
