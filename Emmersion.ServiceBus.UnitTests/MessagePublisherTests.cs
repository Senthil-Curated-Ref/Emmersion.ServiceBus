using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Emmersion.ServiceBus.Pools;
using Emmersion.ServiceBus.SdkWrappers;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests
{
    internal class MessagePublisherTests : With_an_automocked<MessagePublisher>
    {
        [Test]
        public async Task When_publishing_a_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var serviceBusMessage = new ServiceBusMessage();
            GetMock<IServiceBusSenderPool>()
                .Setup(x => x.GetForTopicAsync(topic))
                .ReturnsAsync(GetMock<IServiceBusSender>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<IPublisherConfig>().Setup(x => x.Environment).Returns("unit-tests");

            var before = DateTimeOffset.UtcNow;
            await ClassUnderTest.PublishAsync(message);

            Assert.That(message.PublishedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)), "Incorrect PublishedAt");
            Assert.That(message.EnqueuedAt, Is.EqualTo(message.PublishedAt), "Incorrect EnqueuedAt");
            Assert.That(message.Environment, Is.EqualTo("unit-tests"));
            GetMock<IServiceBusSender>().Verify(x => x.SendAsync(serviceBusMessage));
        }

        [Test]
        public async Task When_publishing_a_message_timing_data_is_emitted()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var serviceBusMessage = new ServiceBusMessage();
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<IServiceBusSenderPool>()
                .Setup(x => x.GetForTopicAsync(topic))
                .ReturnsAsync(GetMock<IServiceBusSender>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<IServiceBusSender>()
                .Setup(x => x.SendAsync(IsAny<ServiceBusMessage>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            await ClassUnderTest.PublishAsync(message);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }

        [Test]
        public async Task When_publishing_a_scheduled_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(5);
            var serviceBusMessage = new ServiceBusMessage();
            GetMock<IServiceBusSenderPool>()
                .Setup(x => x.GetForTopicAsync(topic))
                .ReturnsAsync(GetMock<IServiceBusSender>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<IPublisherConfig>().Setup(x => x.Environment).Returns("unit-tests");

            var before = DateTimeOffset.UtcNow;
            await ClassUnderTest.PublishScheduledAsync(message, enqueueAt);

            Assert.That(message.PublishedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)), "Incorrect PublishedAt");
            Assert.That(message.EnqueuedAt, Is.EqualTo(enqueueAt), "Incorrect EnqueuedAt");
            Assert.That(message.Environment, Is.EqualTo("unit-tests"));
            GetMock<IServiceBusSender>().Verify(x => x.ScheduleMessageAsync(serviceBusMessage, enqueueAt));
        }

        [Test]
        public async Task When_publishing_a_scheduled_message_timing_data_is_emitted()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "test-data" };
            var message = new Message<TestData>(topic, body);
            var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(3);
            var serviceBusMessage = new ServiceBusMessage();
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<IServiceBusSenderPool>()
                .Setup(x => x.GetForTopicAsync(topic))
                .ReturnsAsync(GetMock<IServiceBusSender>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToServiceBusMessage(message)).Returns(serviceBusMessage);
            GetMock<IServiceBusSender>()
                .Setup(x => x.ScheduleMessageAsync(IsAny<ServiceBusMessage>(), IsAny<DateTimeOffset>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            await ClassUnderTest.PublishScheduledAsync(message, enqueueAt);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }

        [Test]
        public async Task When_publishing_a_message_to_the_single_topic()
        {
            var messageEvent = new MessageEvent("test-event", 5);
            var message = new TestData { Data = "I am the very model of a modern major test message." };
            MessageEnvelope<TestData> envelope = null;
            var serviceBusMessage = new ServiceBusMessage();
            GetMock<IServiceBusSenderPool>()
                .Setup(x => x.GetForSingleTopicAsync())
                .ReturnsAsync(GetMock<IServiceBusSender>().Object);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromMessageEnvelope(IsAny<MessageEnvelope<TestData>>()))
                .Callback<MessageEnvelope<TestData>>(x => envelope = x)
                .Returns(serviceBusMessage);
            GetMock<IServiceBusSender>()
                .Setup(x => x.SendAsync(IsAny<ServiceBusMessage>()))
                .Returns(Task.CompletedTask);

            await ClassUnderTest.PublishAsync(messageEvent, message);

            Assert.That(envelope.MessageEvent, Is.EqualTo(messageEvent.ToString()));
            Assert.That(envelope.PublishedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(envelope.Payload, Is.SameAs(message));

            GetMock<IServiceBusSender>().Verify(x => x.SendAsync(serviceBusMessage));
        }

        [Test]
        public async Task When_publishing_a_message_to_the_single_topic_timing_data_is_returned()
        {
            var messageEvent = new MessageEvent("test-event", 13);
            var message = new TestData { Data = "I am the very model of a modern major test message." };
            var receivedTimings = new List<MessagePublishedArgs>();
            GetMock<IServiceBusSenderPool>()
                .Setup(x => x.GetForSingleTopicAsync())
                .ReturnsAsync(GetMock<IServiceBusSender>().Object);
            GetMock<IServiceBusSender>()
                .Setup(x => x.SendAsync(IsAny<ServiceBusMessage>()))
                .Returns(Task.Run(() => Thread.Sleep(150)));

            ClassUnderTest.OnMessagePublished += (object sender, MessagePublishedArgs args) => receivedTimings.Add(args); 
            await ClassUnderTest.PublishAsync(messageEvent, message);

            Assert.That(receivedTimings.Count, Is.EqualTo(1));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
            Assert.That(receivedTimings[0].ElapsedMilliseconds, Is.LessThan(1000));
        }
    }
}