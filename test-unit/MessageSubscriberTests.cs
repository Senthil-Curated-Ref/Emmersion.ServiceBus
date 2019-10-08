using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class MessageSubscriberTests : With_an_automocked<MessageSubscriber>
    {
        [Test]
        public void When_routing_a_message_it_should_only_reach_the_matching_subscriber()
        {
            var serializedMessage = "serialized";
            var deserializedStub = new MessageEnvelope<Stub> { MessageEvent = "test-event.v1", };
            var deserializedMessage = new MessageEnvelope<TestMessage>() { Payload = new TestMessage { Data = "hello world" } };
            var testEventV1Messages = new List<TestMessage>();
            var testEventV2Messages = new List<TestMessage>();
            var otherEventV1Messages = new List<TestMessage>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<TestMessage>>(serializedMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestMessage message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 2), (TestMessage message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("other-event", 1), (TestMessage message) => testEventV1Messages.Add(message));
            
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
            var deserializedStub = new MessageEnvelope<Stub> { MessageEvent = "test-event.v3" };
            var deserializedMessage = new MessageEnvelope<TestMessage>() { Payload = new TestMessage { Data = "hello world" } };
            var subscriber1Messages = new List<TestMessage>();
            var subscriber2Messages = new List<TestMessage>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<TestMessage>>(serializedMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestMessage message) => subscriber1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestMessage message) => subscriber2Messages.Add(message));

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
            var deserializedStub = new MessageEnvelope<Stub> { MessageEvent = "test-event.v3" };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            var before = DateTimeOffset.UtcNow;
            ClassUnderTest.RouteMessage(serializedMessage);
            var duration = DateTimeOffset.UtcNow - before;

            Assert.That(eventArgs.Count, Is.EqualTo(1));
            Assert.That(eventArgs[0].MessageEvent, Is.EqualTo(deserializedStub.MessageEvent));
            Assert.That(eventArgs[0].PublishedAt, Is.EqualTo(deserializedStub.PublishedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.GreaterThan(deserializedStub.PublishedAt));
            Assert.That(eventArgs[0].ProcessingTime, Is.EqualTo(duration).Within(TimeSpan.FromMilliseconds(25)));
        }

        [Test]
        public void When_routing_a_message_you_get_timing_data_even_if_a_subscriber_blows_up()
        {
            var serializedMessage = "serialized";
            var messageEvent = new MessageEvent("test-event", 3);
            var deserializedStub = new MessageEnvelope<Stub> { MessageEvent = messageEvent.ToString() };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);

            ClassUnderTest.Subscribe(messageEvent, (Stub _) => {
                Thread.Sleep(50);
                throw new Exception("Test exception!");
            });

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            var before = DateTimeOffset.UtcNow;
            Assert.Catch<Exception>(() => ClassUnderTest.RouteMessage(serializedMessage));
            var duration = DateTimeOffset.UtcNow - before;

            Assert.That(eventArgs.Count, Is.EqualTo(1));
            Assert.That(eventArgs[0].MessageEvent, Is.EqualTo(deserializedStub.MessageEvent));
            Assert.That(eventArgs[0].PublishedAt, Is.EqualTo(deserializedStub.PublishedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.GreaterThan(deserializedStub.PublishedAt));
            Assert.That(eventArgs[0].ProcessingTime, Is.EqualTo(duration).Within(TimeSpan.FromMilliseconds(25)));
        }

        [Test]
        public void When_routing_a_message_unhandled_exceptions_are_exposed()
        {
            var serializedMessage = "serialized";
            var messageEvent = new MessageEvent("test-event", 3);
            var deserializedStub = new MessageEnvelope<Stub> { MessageEvent = messageEvent.ToString() };
            var exceptionArgs = new List<UnhandledExceptionArgs>();
            var thrownException = new Exception("Test exception!");
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<Stub>>(serializedMessage)).Returns(deserializedStub);

            ClassUnderTest.Subscribe(messageEvent, (Stub _) => throw thrownException);

            ClassUnderTest.OnUnhandledException += (object sender, UnhandledExceptionArgs args) => exceptionArgs.Add(args);
            Assert.Catch<Exception>(() => ClassUnderTest.RouteMessage(serializedMessage));

            Assert.That(exceptionArgs.Count, Is.EqualTo(1));
            Assert.That(exceptionArgs[0].MessageEvent, Is.EqualTo(deserializedStub.MessageEvent));
            Assert.That(exceptionArgs[0].UnhandledException, Is.SameAs(thrownException));
        }
    }
}