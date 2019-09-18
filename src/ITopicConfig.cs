namespace El.ServiceBus
{
    public interface ITopicConfig
    {
        string ConnectionString { get; }
        string TopicName { get; }
    }
}
