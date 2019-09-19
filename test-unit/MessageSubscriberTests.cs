using System;
using System.Collections.Generic;
using El.ServiceBus;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class MessageSubscriberTests : With_an_automocked<MessageSubscriber>
    {
        [Test]
        public void When_routing_a_message_it_should_only_reach_the_matching_subscriber()
        {
            var serializedMessage = "serialized";
            var deserializedStub = new MessageEnvelope<Stub> { EventName = "test-event", EventVersion = 1 };
            var deserializedMessage = new MessageEnvelope<TestMessage>() { Payload = new TestMessage { Data = "hello world" } };
            var testEventV1Messages = new List<TestMessage>();
            var testEventV2Messages = new List<TestMessage>();
            var otherEventV1Messages = new List<TestMessage>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<TestMessage>>(serializedMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe("test-event", 1, (TestMessage message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe("test-event", 2, (TestMessage message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe("other-event", 1, (TestMessage message) => testEventV1Messages.Add(message));
            
            ClassUnderTest.RouteMessage(serializedMessage);

            Assert.That(testEventV1Messages.Count, Is.EqualTo(1));
            Assert.That(testEventV1Messages[0], Is.SameAs(deserializedMessage.Payload));
            Assert.That(testEventV2Messages, Is.Empty);
            Assert.That(otherEventV1Messages, Is.Empty);
        }

        [Test]
        public void When_routing_a_message_and_there_are_multiple_subscribers()
        {
            var serializedMessage = "serialized";
            var deserializedStub = new MessageEnvelope<Stub> { EventName = "test-event", EventVersion = 3 };
            var deserializedMessage = new MessageEnvelope<TestMessage>() { Payload = new TestMessage { Data = "hello world" } };
            var subscriber1Messages = new List<TestMessage>();
            var subscriber2Messages = new List<TestMessage>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<TestMessage>>(serializedMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe("test-event", 3, (TestMessage message) => subscriber1Messages.Add(message));
            ClassUnderTest.Subscribe("test-event", 3, (TestMessage message) => subscriber2Messages.Add(message));

            ClassUnderTest.RouteMessage(serializedMessage);

            Assert.That(subscriber1Messages.Count, Is.EqualTo(1), "Message missing from subscriber 1");
            Assert.That(subscriber1Messages[0], Is.SameAs(deserializedMessage.Payload));
            Assert.That(subscriber2Messages.Count, Is.EqualTo(1), "Message missing from subscriber 2");
            Assert.That(subscriber2Messages[0], Is.SameAs(deserializedMessage.Payload));
        }

        [Test]
        public void When_routing_a_message_you_get_timing_data()
        {
            var serializedMessage = "serialized";
            var deserializedStub = new MessageEnvelope<Stub> { EventName = "test-event", EventVersion = 3 };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            ClassUnderTest.RouteMessage(serializedMessage);

            Assert.That(eventArgs.Count, Is.EqualTo(1));
            Assert.That(eventArgs[0].EventName, Is.EqualTo(deserializedStub.EventName));
            Assert.That(eventArgs[0].EventVersion, Is.EqualTo(deserializedStub.EventVersion));
            Assert.That(eventArgs[0].PublishedAt, Is.EqualTo(deserializedStub.PublishedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.GreaterThan(deserializedStub.PublishedAt));
        }
    }
}