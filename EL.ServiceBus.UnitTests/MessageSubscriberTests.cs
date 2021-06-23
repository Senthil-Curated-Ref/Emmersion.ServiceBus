using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;
using ExceptionReceivedEventArgs = Microsoft.Azure.ServiceBus.ExceptionReceivedEventArgs;

namespace EL.ServiceBus.UnitTests
{
    internal class MessageSubscriberTests : With_an_automocked<MessageSubscriber>
    {
        private Subscription subscription = new Subscription(new Topic("el-service-bus", "test-event", 1), "el-service-bus", "listener");

        [Test]
        public async Task When_subscribing()
        {
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<ISubscriptionClientWrapper>().Object);

            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> message) => Task.CompletedTask);

            GetMock<ISubscriptionClientWrapper>().Verify(x => x.RegisterMessageHandler(
                IsAny<Func<Microsoft.Azure.ServiceBus.Message, Task>>(),
                IsAny<Func<ExceptionReceivedEventArgs, Task>>()));
        }

        [Test]
        public async Task When_handling_a_message()
        {
            Func<Microsoft.Azure.ServiceBus.Message, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var message = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"});
            var receivedAt = DateTimeOffset.MinValue;
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(IsAny<Func<Microsoft.Azure.ServiceBus.Message, Task>>(), IsAny<Func<ExceptionReceivedEventArgs, Task>>()))
                .Callback<Func<Microsoft.Azure.ServiceBus.Message, Task>, Func<ExceptionReceivedEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessage, IsAny<DateTimeOffset>()))
                .Callback<Topic, Microsoft.Azure.ServiceBus.Message, DateTimeOffset>((x, y, z) => receivedAt = z)
                .Returns(message);

            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> message) =>
            {
                receivedMessages.Add(message);
                return Task.CompletedTask;
            });
            var before = DateTimeOffset.UtcNow;
            await messageHandler(serviceBusMessage);

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(message));
            Assert.That(receivedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public async Task When_handling_a_message_timing_data_is_emitted()
        {
            Func<Microsoft.Azure.ServiceBus.Message, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var message = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"})
            {
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-3)
            };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(IsAny<Func<Microsoft.Azure.ServiceBus.Message, Task>>(), IsAny<Func<ExceptionReceivedEventArgs, Task>>()))
                .Callback<Func<Microsoft.Azure.ServiceBus.Message, Task>, Func<ExceptionReceivedEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessage, IsAny<DateTimeOffset>()))
                .Returns(message);

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> message) =>
            {
                receivedMessages.Add(message);
                Thread.Sleep(150);
                return Task.CompletedTask;
            });
            var before = DateTimeOffset.UtcNow;
            await messageHandler(serviceBusMessage);
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
        public async Task When_handling_a_message_that_should_be_filtered_out()
        {
            Func<Microsoft.Azure.ServiceBus.Message, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessageToReceive = new Microsoft.Azure.ServiceBus.Message();
            var serviceBusMessageToFilterOut = new Microsoft.Azure.ServiceBus.Message();
            var messageToReceive = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"}) { Environment = "unit-tests" };
            var messageToFilterOut = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"}) { Environment = "other-env"};
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(IsAny<Func<Microsoft.Azure.ServiceBus.Message, Task>>(), IsAny<Func<ExceptionReceivedEventArgs, Task>>()))
                .Callback<Func<Microsoft.Azure.ServiceBus.Message, Task>, Func<ExceptionReceivedEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessageToReceive, IsAny<DateTimeOffset>()))
                .Returns(messageToReceive);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessageToFilterOut, IsAny<DateTimeOffset>()))
                .Returns(messageToFilterOut);
            GetMock<ISubscriptionConfig>().Setup(x => x.EnvironmentFilter).Returns("unit-tests");

            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> message) =>
            {
                receivedMessages.Add(message);
                return Task.CompletedTask;
            });
            await messageHandler(serviceBusMessageToReceive);
            await messageHandler(serviceBusMessageToFilterOut);

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(messageToReceive));
        }

        [Test]
        public async Task When_handling_a_message_timing_data_is_emitted_even_if_the_subscribed_handler_throws()
        {
            Func<Microsoft.Azure.ServiceBus.Message, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var message = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"})
            {
                PublishedAt = DateTimeOffset.UtcNow.AddSeconds(-3),
                EnqueuedAt = DateTimeOffset.UtcNow.AddSeconds(-1)
            };
            var eventArgs = new List<MessageReceivedArgs>();
            var testException = new Exception("test exception");
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(IsAny<Func<Microsoft.Azure.ServiceBus.Message, Task>>(), IsAny<Func<ExceptionReceivedEventArgs, Task>>()))
                .Callback<Func<Microsoft.Azure.ServiceBus.Message, Task>, Func<ExceptionReceivedEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessage, IsAny<DateTimeOffset>()))
                .Returns(message);

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            await ClassUnderTest.SubscribeAsync(subscription, async (Message<TestData> x) =>
            {
                receivedMessages.Add(x);
                await Task.Delay(150);
                throw testException;
            });
            var before = DateTimeOffset.UtcNow;
            var caught = Assert.CatchAsync(() => messageHandler(serviceBusMessage));
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
        public async Task When_handling_exceptions()
        {
            Func<ExceptionReceivedEventArgs, Task> exceptionHandler = null;
            var exceptionArgs = new List<ExceptionArgs>();
            var serviceBusExceptionArgs = new ExceptionReceivedEventArgs(new Exception("test exception"), "action", "endpoint", "entity name", "client id");
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(IsAny<Func<Microsoft.Azure.ServiceBus.Message, Task>>(), IsAny<Func<ExceptionReceivedEventArgs, Task>>()))
                .Callback<Func<Microsoft.Azure.ServiceBus.Message, Task>, Func<ExceptionReceivedEventArgs, Task>>((_, handler) => exceptionHandler = handler);

            ClassUnderTest.OnException += (object sender, ExceptionArgs args) => exceptionArgs.Add(args);
            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> message) => Task.CompletedTask);
            await exceptionHandler(serviceBusExceptionArgs);
            
            Assert.That(exceptionArgs.Count, Is.EqualTo(1));
            Assert.That(exceptionArgs[0].Subscription, Is.SameAs(subscription));
            Assert.That(exceptionArgs[0].Exception, Is.SameAs(serviceBusExceptionArgs.Exception));
            Assert.That(exceptionArgs[0].Action, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.Action));
            Assert.That(exceptionArgs[0].Endpoint, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.Endpoint));
            Assert.That(exceptionArgs[0].EntityPath, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.EntityPath));
            Assert.That(exceptionArgs[0].ClientId, Is.EqualTo(serviceBusExceptionArgs.ExceptionReceivedContext.ClientId));
        }

        [Test]
        public async Task When_subscribing_to_the_dead_letter_queue()
        {
            Func<Microsoft.Azure.ServiceBus.Message, Task> messageHandler = null;
            var testMessage = new Microsoft.Azure.ServiceBus.Message();
            var expectedDeadLetter = new DeadLetter();
            DeadLetter deadLetter = null;
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetDeadLetterClientAsync(subscription))
                .ReturnsAsync(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(IsAny<Func<Microsoft.Azure.ServiceBus.Message, Task>>(), IsAny<Func<ExceptionReceivedEventArgs, Task>>()))
                .Callback<Func<Microsoft.Azure.ServiceBus.Message, Task>, Func<ExceptionReceivedEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>().Setup(x => x.GetDeadLetter(testMessage)).Returns(expectedDeadLetter);

            await ClassUnderTest.SubscribeToDeadLettersAsync(subscription, (DeadLetter x) =>
            {
                deadLetter = x;
                return Task.CompletedTask;
            });
            await messageHandler(testMessage);
            
            Assert.That(deadLetter, Is.EqualTo(expectedDeadLetter));
        }

        [Test]
        public void When_subscribing_to_a_single_topic_message_event()
        {
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => {});

            GetMock<ISubscriptionClientWrapper>().Verify(x => x.RegisterMessageHandler(ClassUnderTest.RouteMessage, IsAny<Func<ExceptionReceivedEventArgs, Task>>()));
        }

        [Test]
        public void When_subscribing_to_a_single_topic_message_event_multiple_times()
        {
            GetMock<ISubscriptionClientWrapperPool>()
                .SetupSequence(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<ISubscriptionClientWrapper>().Object)
                .Returns((ISubscriptionClientWrapper)null);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => {});
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 2), (TestData message) => {});

            GetMock<ISubscriptionClientWrapper>().Verify(x => x.RegisterMessageHandler(ClassUnderTest.RouteMessage, IsAny<Func<ExceptionReceivedEventArgs, Task>>()), Times.Once);
        }

        [Test]
        public void When_routing_a_message_it_should_only_reach_the_matching_subscriber()
        {
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message();
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = "test-event.v1", };
            var deserializedMessage = new MessageEnvelope<TestData>() { Payload = new TestData { Data = "hello world" } };
            var testEventV1Messages = new List<TestData>();
            var testEventV2Messages = new List<TestData>();
            var otherEventV1Messages = new List<TestData>();
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<TestData>(serviceBusMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 2), (TestData message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("other-event", 1), (TestData message) => testEventV1Messages.Add(message));

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
            var deserializedMessage = new MessageEnvelope<TestData>() { Payload = new TestData { Data = "hello world" } };
            var subscriber1Messages = new List<TestData>();
            var subscriber2Messages = new List<TestData>();
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<TestData>(serviceBusMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestData message) => subscriber1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestData message) => subscriber2Messages.Add(message));

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
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
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
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);

            ClassUnderTest.Subscribe(messageEvent, (object _) =>
            {
                Thread.Sleep(50);
                throw new Exception("Test exception!");
            });

            ClassUnderTest.OnMessageReceived += (object sender, MessageReceivedArgs args) => eventArgs.Add(args);
            var before = DateTimeOffset.UtcNow;
            Assert.CatchAsync<Exception>(() => ClassUnderTest.RouteMessage(serviceBusMessage));
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
            var eventArgs = new List<ExceptionArgs>();
            Func<ExceptionReceivedEventArgs, Task> exceptionHandler = null;
            GetMock<ISubscriptionClientWrapperPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(ClassUnderTest.RouteMessage, IsAny<Func<ExceptionReceivedEventArgs, Task>>()))
                .Callback<Func<Microsoft.Azure.ServiceBus.Message, Task>, Func<ExceptionReceivedEventArgs, Task>>((_, handler) => exceptionHandler = handler);

            ClassUnderTest.OnException += (_, args) => eventArgs.Add(args);
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => {});
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