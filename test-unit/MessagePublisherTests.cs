using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class MessagePublisherTests : With_an_automocked<MessagePublisher>
    {
        [Test]
        public void When_publishing_a_message()
        {
            var messageEvent = new MessageEvent("test-event", 5);
            var message = new TestMessage { Data = "I am the very model of a modern major test message." };
            MessageEnvelope<TestMessage> envelope = null;
            var serializedString = "pretend this is json";
            Message sentMessage = null;
            GetMock<IMessageSerializer>()
                .Setup(x => x.Serialize(Any<MessageEnvelope<TestMessage>>()))
                .Callback<MessageEnvelope<TestMessage>>(x => envelope = x)
                .Returns(serializedString);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(Any<Message>()))
                .Callback<Message>(x => sentMessage = x)
                .Returns(Task.CompletedTask);

            ClassUnderTest.Publish(messageEvent, message);

            Assert.That(envelope.MessageEvent, Is.EqualTo(messageEvent.ToString()));
            Assert.That(envelope.PublishedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(envelope.Payload, Is.SameAs(message));

            Assert.That(sentMessage.Body, Is.EqualTo(Encoding.UTF8.GetBytes(serializedString)));
        }

        [Test]
        public void When_publishing_a_message_timing_data_is_returned()
        {
            var messageEvent = new MessageEvent("test-event", 13);
            var message = new TestMessage { Data = "I am the very model of a modern major test message." };
            var serializedString = "pretend this is json";
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<IMessageSerializer>()
                .Setup(x => x.Serialize(Any<MessageEnvelope<TestMessage>>()))
                .Returns(serializedString);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(Any<Message>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            ClassUnderTest.Publish(messageEvent, message);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(150));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }

        [Test]
        public void When_disposing()
        {
            ClassUnderTest.Dispose();

            GetMock<ITopicClientWrapper>().Verify(x => x.CloseAsync());
        }
    }
}