using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class MessagePublisherTests : With_an_automocked<MessagePublisher>
    {
        [Test]
        public void When_publishing_a_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestMessage { Data = "test-data" };
            var message = new Message<TestMessage>(topic, body);
            var connectionString = "connection-string";
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(connectionString, topic.ToString()))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);

            ClassUnderTest.Publish(message);

            GetMock<ITopicClientWrapper>().Verify(x => x.SendAsync(serviceBusMessage));
        }

        [Test]
        public void When_publishing_a_message_timing_data_is_emitted()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestMessage { Data = "test-data" };
            var message = new Message<TestMessage>(topic, body);
            var connectionString = "connection-string";
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(connectionString, topic.ToString()))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(Any<Microsoft.Azure.ServiceBus.Message>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            ClassUnderTest.Publish(message);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(150));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }

        [Test]
        public void When_publishing_a_scheduled_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestMessage { Data = "test-data" };
            var message = new Message<TestMessage>(topic, body);
            var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(5);
            var connectionString = "connection-string";
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(connectionString, topic.ToString()))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);

            ClassUnderTest.PublishScheduled(message, enqueueAt);

            GetMock<ITopicClientWrapper>().Verify(x => x.ScheduleMessageAsync(serviceBusMessage, enqueueAt));
        }

        [Test]
        public void When_publishing_a_scheduled_message_timing_data_is_emitted()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestMessage { Data = "test-data" };
            var message = new Message<TestMessage>(topic, body);
            var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(3);
            var connectionString = "connection-string";
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(connectionString, topic.ToString()))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.ScheduleMessageAsync(Any<Microsoft.Azure.ServiceBus.Message>(), Any<DateTimeOffset>()))
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
            var message = new TestMessage { Data = "I am the very model of a modern major test message." };
            MessageEnvelope<TestMessage> envelope = null;
            var serializedString = "pretend this is json";
            Microsoft.Azure.ServiceBus.Message sentMessage = null;
            var singleTopicConnectionString = "single-topic-connnection-string";
            var singleTopicName = "single-topic-name";
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicConnectionString).Returns(singleTopicConnectionString);
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicName).Returns(singleTopicName);
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(singleTopicConnectionString, singleTopicName))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageSerializer>()
                .Setup(x => x.Serialize(Any<MessageEnvelope<TestMessage>>()))
                .Callback<MessageEnvelope<TestMessage>>(x => envelope = x)
                .Returns(serializedString);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(Any<Microsoft.Azure.ServiceBus.Message>()))
                .Callback<Microsoft.Azure.ServiceBus.Message>(x => sentMessage = x)
                .Returns(Task.CompletedTask);

            ClassUnderTest.Publish(messageEvent, message);

            Assert.That(envelope.MessageEvent, Is.EqualTo(messageEvent.ToString()));
            Assert.That(envelope.PublishedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(envelope.Payload, Is.SameAs(message));

            Assert.That(sentMessage.Body, Is.EqualTo(Encoding.UTF8.GetBytes(serializedString)));
        }

        [Test]
        public void When_publishing_a_message_to_the_single_topic_timing_data_is_returned()
        {
            var messageEvent = new MessageEvent("test-event", 13);
            var message = new TestMessage { Data = "I am the very model of a modern major test message." };
            var serializedString = "pretend this is json";
            var receivedTimings = new List<MessagePublishedArgs>();
            var singleTopicConnectionString = "single-topic-connnection-string";
            var singleTopicName = "single-topic-name";
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicConnectionString).Returns(singleTopicConnectionString);
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicName).Returns(singleTopicName);
            GetMock<ITopicClientWrapperPool>()
                .Setup(x => x.GetForTopic(singleTopicConnectionString, singleTopicName))
                .Returns(GetMock<ITopicClientWrapper>().Object);
            GetMock<IMessageSerializer>()
                .Setup(x => x.Serialize(Any<MessageEnvelope<TestMessage>>()))
                .Returns(serializedString);
            GetMock<ITopicClientWrapper>()
                .Setup(x => x.SendAsync(Any<Microsoft.Azure.ServiceBus.Message>()))
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

            GetMock<ITopicClientWrapperPool>().Verify(x => x.Dispose());
        }
    }
}