using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EL.Testing;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class MessagePublisherTests : With_an_automocked<MessagePublisher>
    {
        [Test]
        public void When_publishing_a_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(topic))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<IPublisherConfig>().Setup(x => x.Environment).Returns("unit-tests");

            var before = DateTimeOffset.UtcNow;
            ClassUnderTest.Publish(message);

            Assert.That(message.PublishedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)), "Incorrect PublishedAt");
            Assert.That(message.EnqueuedAt, Is.EqualTo(message.PublishedAt), "Incorrect EnqueuedAt");
            Assert.That(message.Environment, Is.EqualTo("unit-tests"));
            GetMock<ITopicClientWrapper>().Verify(x => x.SendAsync(serviceBusMessage));
        }

        [Test]
        public void When_publishing_a_message_timing_data_is_emitted()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(topic))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(IsAny<Microsoft.Azure.ServiceBus.Message>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            ClassUnderTest.Publish(message);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }

        [Test]
        public void When_publishing_a_scheduled_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(5);
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(topic))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<IPublisherConfig>().Setup(x => x.Environment).Returns("unit-tests");

            var before = DateTimeOffset.UtcNow;
            ClassUnderTest.PublishScheduled(message, enqueueAt);

            Assert.That(message.PublishedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)), "Incorrect PublishedAt");
            Assert.That(message.EnqueuedAt, Is.EqualTo(enqueueAt), "Incorrect EnqueuedAt");
            Assert.That(message.Environment, Is.EqualTo("unit-tests"));
            GetMock<ITopicClientWrapper>().Verify(x => x.ScheduleMessageAsync(serviceBusMessage, enqueueAt));
        }

        [Test]
        public void When_publishing_a_scheduled_message_timing_data_is_emitted()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(3);
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(topic))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.ScheduleMessageAsync(IsAny<Microsoft.Azure.ServiceBus.Message>(), IsAny<DateTimeOffset>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            ClassUnderTest.PublishScheduled(message, enqueueAt);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(150));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }

        [Test]
        public void When_publishing_a_message_to_the_single_topic()
        {
            var messageEvent = new MessageEvent("test-event", 5);
            var message = new TestData { Data = "I am the very model of a modern major test message." };
            MessageEnvelope<TestData> envelope = null;
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForSingleTopic())
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromMessageEnvelope(IsAny<MessageEnvelope<TestData>>()))
                .Callback<MessageEnvelope<TestData>>(x => envelope = x)
                .Returns(serviceBusMessage);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(IsAny<Microsoft.Azure.ServiceBus.Message>()))
                .Returns(Task.CompletedTask);

            ClassUnderTest.Publish(messageEvent, message);

            Assert.That(envelope.MessageEvent, Is.EqualTo(messageEvent.ToString()));
            Assert.That(envelope.PublishedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(envelope.Payload, Is.SameAs(message));

            GetMock<ITopicClientWrapper>().Verify(x => x.SendAsync(serviceBusMessage));
        }

        [Test]
        public void When_publishing_a_message_to_the_single_topic_timing_data_is_returned()
        {
            var messageEvent = new MessageEvent("test-event", 13);
            var message = new TestData { Data = "I am the very model of a modern major test message." };
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForSingleTopic())
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(IsAny<Microsoft.Azure.ServiceBus.Message>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            ClassUnderTest.Publish(messageEvent, message);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }
    }
}