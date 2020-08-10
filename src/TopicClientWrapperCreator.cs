namespace EL.ServiceBus
{
    internal interface ITopicClientWrapperCreator
    {
        ITopicClientWrapper Create(string connectionString, string topicName);
    }

    internal class TopicClientWrapperCreator : ITopicClientWrapperCreator
    {
        public ITopicClientWrapper Create(string connectionString, string topicName)
        {
            return new TopicClientWrapper(connectionString, topicName);
        }
    }
}
