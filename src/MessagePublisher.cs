using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    public interface IMessagePublisher : IDisposable
    {
        void Publish<T>(MessageEvent messageEvent, T message);
        event OnMessagePublished OnMessagePublished;
    }

    internal class MessagePublisher : IMessagePublisher
    {
        private readonly ITopicClientWrapper client;
        private readonly IMessageSerializer serializer;
        public event OnMessagePublished OnMessagePublished;

        public MessagePublisher(ITopicClientWrapper topicClientWrapper, IMessageSerializer serializer)
        {
            this.client = topicClientWrapper;
            this.serializer = serializer;
        }

        public void Dispose()
        {
            client.CloseAsync();
        }

        public void Publish<T>(MessageEvent messageEvent, T message)
        {
            var stopwatch = Stopwatch.StartNew();
            var envelope = new MessageEnvelope<T> {
                MessageEvent = messageEvent.ToString(),
                Payload = message
            };
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            Task.WaitAll(client.SendAsync(new Message(bytes)));
            OnMessagePublished?.Invoke(this, new MessagePublishedArgs(stopwatch.ElapsedMilliseconds));
        }
    }
}
