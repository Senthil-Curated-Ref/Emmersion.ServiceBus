using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace El.ServiceBus
{
    public interface IMessagePublisher : IDisposable
    {
        void Publish<T>(string eventName, uint version, T message);
        event OnMessagePublished OnMessagePublished;
    }

    internal class MessagePublisher : IMessagePublisher
    {
        private readonly TopicClient client;
        private readonly IMessageSerializer serializer;
        public event OnMessagePublished OnMessagePublished;

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
            var stopwatch = Stopwatch.StartNew();
            var envelope = new MessageEnvelope<T> {
                EventName = eventName,
                EventVersion = version,
                Payload = message
            };
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            Task.WaitAll(client.SendAsync(new Message(bytes)));
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }
    }
}
