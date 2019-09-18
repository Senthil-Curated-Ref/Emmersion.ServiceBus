using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace El.ServiceBus
{
    public interface IMessagePublisher : IDisposable
    {
        void Publish<T>(string eventName, uint version, T message);
    }

    internal class MessagePublisher : IMessagePublisher
    {
        private readonly TopicClient client;
        private readonly IMessageSerializer serializer;

        public MessagePublisher(ITopicConfig config, IMessageSerializer serializer)
        {
            this.serializer = serializer;
            client = new TopicClient(config.ConnectionString, config.TopicName);
        }

        public void Dispose()
        {
            client.CloseAsync();
        }

        public void Publish<T>(string eventName, uint version, T message)
        {
            var envelope = new MessageEnvelope<T> {
                EventName = eventName,
                EventVersion = version,
                Payload = message
            };
            Task.WaitAll(client.SendAsync(new Message(serializer.Serialize(envelope))));
        }
    }
}
