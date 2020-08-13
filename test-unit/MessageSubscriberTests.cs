using System;
using System.Collections.Generic;
using System.Threading;
using Moq;
using NUnit.Framework;
using ExceptionReceivedEventArgs = Microsoft.Azure.ServiceBus.ExceptionReceivedEventArgs;

namespace EL.ServiceBus.UnitTests
{
    internal class MessageSubscriberTests : With_an_automocked<MessageSubscriber>
    {
        private Subscription subscription = new Subscription(new Topic("el-service-bus", "test-event", 1), "el-service-bus", "listener");

        [Test]
        public void When_subscribing()
        {
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);

            ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) => {});

            GetMock<ISubscriptionClientWrapper>().Verify(x => x.RegisterMessageHandler(
                Any<Action<Microsoft.Azure.ServiceBus.Message>>(),
                Any<Action<ExceptionReceivedEventArgs>>()));
        }

        [Test]
        public void When_subscribing_and_there_is_already_an_instance_of_the_subscription()
        {
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);

            ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) => {});

            var exception = Assert.Catch(() => ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) => {}));

            Assert.That(exception.Message, Is.EqualTo("Connecting to the same subscription twice is not allowed."));
        }

        [Test]
        public void When_handling_a_message()
        {
            Action<Microsoft.Azure.ServiceBus.Message> messageHandler = null;
            var receivedMessages = new List<Message<TestMessage>>();
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var message = new Message<TestMessage>(subscription.Topic, new TestMessage { Data = "test data"});
            var receivedAt = DateTimeOffset.MinValue;
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(Any<Action<Microsoft.Azure.ServiceBus.Message>>(), Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestMessage>(subscription.Topic, serviceBusMessage, Any<DateTimeOffset>()))
                .Callback<Topic, Microsoft.Azure.ServiceBus.Message, DateTimeOffset>((x, y, z) => receivedAt = z)
                .Returns(message);

            ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) => receivedMessages.Add(message));
            var before = DateTimeOffset.UtcNow;
            messageHandler(serviceBusMessage);

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(message));
            Assert.That(receivedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public void When_handling_a_message_timing_data_is_emitted()
        {
            Action<Microsoft.Azure.ServiceBus.Message> messageHandler = null;
            var receivedMessages = new List<Message<TestMessage>>();
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var message = new Message<TestMessage>(subscription.Topic, new TestMessage { Data = "test data"})
            {
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-3)
            };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(Any<Action<Microsoft.Azure.ServiceBus.Message>>(), Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestMessage>(subscription.Topic, serviceBusMessage, Any<DateTimeOffset>()))
                .Returns(message);

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) =>
            {
                receivedMessages.Add(message);
                Thread.Sleep(150);
            });
            var before = DateTimeOffset.UtcNow;
            messageHandler(serviceBusMessage);
            var after = DateTimeOffset.UtcNow;
            var duration = after - before;

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(message));

            Assert.That(eventArgs.Count, Is.EqualTo(1), "No eventArgs were received");
            Assert.That(eventArgs[0].Subscription, Is.EqualTo(subscription));
            Assert.That(eventArgs[0].MessageEvent, Is.Null);
            Assert.That(eventArgs[0].PublishedAt, Is.EqualTo(message.PublishedAt));
            Assert.That(eventArgs[0].EnqueuedAt, Is.EqualTo(message.EnqueuedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)));
            Assert.That(eventArgs[0].ProcessingTime, Is.EqualTo(duration).Within(TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public void When_handling_a_message_timing_data_is_emitted_even_if_the_subscribed_action_throws()
        {
            Action<Microsoft.Azure.ServiceBus.Message> messageHandler = null;
            var receivedMessages = new List<Message<TestMessage>>();
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var message = new Message<TestMessage>(subscription.Topic, new TestMessage { Data = "test data"})
            {
                PublishedAt = DateTimeOffset.UtcNow.AddSeconds(-3),
                EnqueuedAt = DateTimeOffset.UtcNow.AddSeconds(-1)
            };
            var eventArgs = new List<MessageReceivedArgs>();
            var testException = new Exception("test exception");
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(Any<Action<Microsoft.Azure.ServiceBus.Message>>(), Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestMessage>(subscription.Topic, serviceBusMessage, Any<DateTimeOffset>()))
                .Returns(message);

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) =>
            {
                receivedMessages.Add(message);
                Thread.Sleep(150);
                throw testException;
            });
            var before = DateTimeOffset.UtcNow;
            var caught = Assert.Catch(() => messageHandler(serviceBusMessage));
            var after = DateTimeOffset.UtcNow;
            var duration = after - before;

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(message));

            Assert.That(eventArgs.Count, Is.EqualTo(1), "No eventArgs were received");
            Assert.That(eventArgs[0].Subscription, Is.EqualTo(subscription));
            Assert.That(eventArgs[0].MessageEvent, Is.Null);
            Assert.That(eventArgs[0].PublishedAt, Is.EqualTo(message.PublishedAt));
            Assert.That(eventArgs[0].EnqueuedAt, Is.EqualTo(message.EnqueuedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)));
            Assert.That(eventArgs[0].ProcessingTime, Is.EqualTo(duration).Within(TimeSpan.FromMilliseconds(10)));

            Assert.That(caught, Is.SameAs(testException));
        }

        [Test]
        public void When_handling_exceptions()
        {
            Action<ExceptionReceivedEventArgs> exceptionHandler = null;
            var exceptionArgs = new List<ServiceBusExceptionArgs>();
            var serviceBusExceptionArgs = new ExceptionReceivedEventArgs(new Exception("test exception"), "action", "endpoint", "entity name", "client id");
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(Any<Action<Microsoft.Azure.ServiceBus.Message>>(), Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((_, handler) => exceptionHandler = handler);

            ClassUnderTest.OnServiceBusException += (object sender, ServiceBusExceptionArgs args) => exceptionArgs.Add(args);
            ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) => {});
            exceptionHandler(serviceBusExceptionArgs);
            
            Assert.That(exceptionArgs.Count, Is.EqualTo(1));
            Assert.That(exceptionArgs[0].Subscription, Is.SameAs(subscription));
            Assert.That(exceptionArgs[0].Exception, Is.SameAs(serviceBusExceptionArgs.Exception));
            Assert.That(exceptionArgs[0].Action, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.Action));
            Assert.That(exceptionArgs[0].Endpoint, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.Endpoint));
            Assert.That(exceptionArgs[0].EntityPath, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.EntityPath));
            Assert.That(exceptionArgs[0].ClientId, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.ClientId));
        }

        [Test]
        public void When_subscribing_to_a_single_topic_message_event()
        {
            GetMock<ISubscriptionClientWrapperCreator>().Setup(x => x.CreateSingleTopic()).Returns(GetMock<ISubscriptionClientWrapper>().Object);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestMessage message) => {});

            GetMock<ISubscriptionClientWrapper>().Verify(x => x.RegisterMessageHandler(ClassUnderTest.RouteMessage, Any<Action<ExceptionReceivedEventArgs>>()));
        }

        [Test]
        public void When_subscribing_to_a_single_topic_message_event_multiple_times()
        {
            GetMock<ISubscriptionClientWrapperCreator>().Setup(x => x.CreateSingleTopic()).Returns(GetMock<ISubscriptionClientWrapper>().Object);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestMessage message) => {});
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 2), (TestMessage message) => {});

            GetMock<ISubscriptionClientWrapperCreator>().Verify(x => x.CreateSingleTopic(), Times.Once);
            GetMock<ISubscriptionClientWrapper>().Verify(x => x.RegisterMessageHandler(ClassUnderTest.RouteMessage, Any<Action<ExceptionReceivedEventArgs>>()), Times.Once);
        }

        [Test]
        public void When_routing_a_message_it_should_only_reach_the_matching_subscriber()
        {
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = "test-event.v1", };
            var deserializedMessage = new MessageEnvelope<TestMessage>() { Payload = new TestMessage { Data = "hello world" } };
            var testEventV1Messages = new List<TestMessage>();
            var testEventV2Messages = new List<TestMessage>();
            var otherEventV1Messages = new List<TestMessage>();
            GetMock<ISubscriptionClientWrapperCreator>().Setup(x => x.CreateSingleTopic()).Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<TestMessage>(serviceBusMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestMessage message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 2), (TestMessage message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("other-event", 1), (TestMessage message) => testEventV1Messages.Add(message));

            ClassUnderTest.RouteMessage(serviceBusMessage);

            Assert.That(testEventV1Messages.Count, Is.EqualTo(1));
            Assert.That(testEventV1Messages[0], Is.SameAs(deserializedMessage.Payload));
            Assert.That(testEventV2Messages, Is.Empty);
            Assert.That(otherEventV1Messages, Is.Empty);
        }

        [Test]
        public void When_routing_a_message_and_there_are_multiple_subscribers()
        {
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = "test-event.v3" };
            var deserializedMessage = new MessageEnvelope<TestMessage>() { Payload = new TestMessage { Data = "hello world" } };
            var subscriber1Messages = new List<TestMessage>();
            var subscriber2Messages = new List<TestMessage>();
            GetMock<ISubscriptionClientWrapperCreator>().Setup(x => x.CreateSingleTopic()).Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<TestMessage>(serviceBusMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestMessage message) => subscriber1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestMessage message) => subscriber2Messages.Add(message));

            ClassUnderTest.RouteMessage(serviceBusMessage);

            Assert.That(subscriber1Messages.Count, Is.EqualTo(1), "Message missing from subscriber 1");
            Assert.That(subscriber1Messages[0], Is.SameAs(deserializedMessage.Payload));
            Assert.That(subscriber2Messages.Count, Is.EqualTo(1), "Message missing from subscriber 2");
            Assert.That(subscriber2Messages[0], Is.SameAs(deserializedMessage.Payload));
        }

        [Test]
        public void When_routing_a_message_you_get_timing_data()
        {
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = "test-event.v3" };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<ISubscriptionClientWrapperCreator>().Setup(x => x.CreateSingleTopic()).Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            var before = DateTimeOffset.UtcNow;
            ClassUnderTest.RouteMessage(serviceBusMessage);
            var duration = DateTimeOffset.UtcNow - before;

            Assert.That(eventArgs.Count, Is.EqualTo(1));
            Assert.That(eventArgs[0].Subscription, Is.Null);
            Assert.That(eventArgs[0].MessageEvent, Is.EqualTo(deserializedObject.MessageEvent));
            Assert.That(eventArgs[0].PublishedAt, Is.EqualTo(deserializedObject.PublishedAt));
            Assert.That(eventArgs[0].EnqueuedAt, Is.EqualTo(deserializedObject.PublishedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.GreaterThan(deserializedObject.PublishedAt));
            Assert.That(eventArgs[0].ProcessingTime, Is.EqualTo(duration).Within(TimeSpan.FromMilliseconds(25)));

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (object _) => { Thread.Sleep(25); });
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (object _) => { Thread.Sleep(25); });
            ClassUnderTest.RouteMessage(serviceBusMessage);

            Assert.That(eventArgs.Count, Is.EqualTo(2));
            Assert.That(eventArgs[1].ProcessingTime.TotalMilliseconds, Is.EqualTo(50).Within(20));
        }

        [Test]
        public void When_routing_a_message_you_get_timing_data_even_if_a_subscriber_blows_up()
        {
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var messageEvent = new MessageEvent("test-event", 3);
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = messageEvent.ToString() };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<ISubscriptionClientWrapperCreator>().Setup(x => x.CreateSingleTopic()).Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);

            ClassUnderTest.Subscribe(messageEvent, (object _) =>
            {
                Thread.Sleep(50);
                throw new Exception("Test exception!");
            });

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            var before = DateTimeOffset.UtcNow;
            Assert.Catch<Exception>(() => ClassUnderTest.RouteMessage(serviceBusMessage));
            var duration = DateTimeOffset.UtcNow - before;

            Assert.That(eventArgs.Count, Is.EqualTo(1));
            Assert.That(eventArgs[0].Subscription, Is.Null);
            Assert.That(eventArgs[0].MessageEvent, Is.EqualTo(deserializedObject.MessageEvent));
            Assert.That(eventArgs[0].PublishedAt, Is.EqualTo(deserializedObject.PublishedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.GreaterThan(deserializedObject.PublishedAt));
            Assert.That(eventArgs[0].ProcessingTime, Is.EqualTo(duration).Within(TimeSpan.FromMilliseconds(25)));
        }

        [Test]
        public void When_handling_service_bus_exceptions_for_single_topic_subscriptions()
        {
            var serviceBusArgs = new ExceptionReceivedEventArgs(new Exception("test exception"), "action", "endpoint", "entity name", "client id");
            var eventArgs = new List<ServiceBusExceptionArgs>();
            Action<ExceptionReceivedEventArgs> exceptionHandler = null;
            GetMock<ISubscriptionClientWrapperCreator>().Setup(x => x.CreateSingleTopic()).Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(ClassUnderTest.RouteMessage, Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((_, handler) => exceptionHandler = handler);

            ClassUnderTest.OnServiceBusException += (_, args) => eventArgs.Add(args);
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestMessage message) => {});
            exceptionHandler(serviceBusArgs);

            Assert.That(eventArgs.Count, Is.EqualTo(1));
            Assert.That(eventArgs[0].Subscription, Is.Null);
            Assert.That(eventArgs[0].Exception, Is.SameAs(serviceBusArgs.Exception));
            Assert.That(eventArgs[0].Action, Is.EqualTo("action"));
            Assert.That(eventArgs[0].Endpoint, Is.EqualTo("endpoint"));
            Assert.That(eventArgs[0].EntityPath, Is.EqualTo("entity name"));
            Assert.That(eventArgs[0].ClientId, Is.EqualTo("client id"));
        }
    }
}