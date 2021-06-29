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
    internal class MessageSubscriberTests : With_an_automocked<MessageSubscriber>
    {
        private Subscription subscription = new Subscription(new Topic("el-service-bus", "test-event", 1), "el-service-bus", "listener");

        [Test]
        public async Task When_subscribing()
        {
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<IServiceBusProcessor>().Object);

            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> message) => Task.CompletedTask);

            GetMock<IServiceBusProcessor>().Verify(x => x.RegisterMessageHandlerAsync(
                IsAny<Func<ProcessMessageEventArgs, Task>>(),
                IsAny<Func<ProcessErrorEventArgs, Task>>()));
        }

        [Test]
        public async Task When_handling_a_message()
        {
            Func<ProcessMessageEventArgs, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var message = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"});
            var receivedAt = DateTimeOffset.MinValue;
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<IServiceBusProcessor>().Object);
            GetMock<IServiceBusProcessor>()
                .Setup(x => x.RegisterMessageHandlerAsync(IsAny<Func<ProcessMessageEventArgs, Task>>(), IsAny<Func<ProcessErrorEventArgs, Task>>()))
                .Callback<Func<ProcessMessageEventArgs, Task>, Func<ProcessErrorEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessage, IsAny<DateTimeOffset>()))
                .Callback<Topic, ServiceBusReceivedMessage, DateTimeOffset>((x, y, z) => receivedAt = z)
                .Returns(message);

            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> m) =>
            {
                receivedMessages.Add(m);
                return Task.CompletedTask;
            });
            var before = DateTimeOffset.UtcNow;
            await messageHandler(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None));

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(message));
            Assert.That(receivedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public async Task When_handling_a_message_timing_data_is_emitted()
        {
            Func<ProcessMessageEventArgs, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var message = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"})
            {
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-3)
            };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<IServiceBusProcessor>().Object);
            GetMock<IServiceBusProcessor>()
                .Setup(x => x.RegisterMessageHandlerAsync(IsAny<Func<ProcessMessageEventArgs, Task>>(), IsAny<Func<ProcessErrorEventArgs, Task>>()))
                .Callback<Func<ProcessMessageEventArgs, Task>, Func<ProcessErrorEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessage, IsAny<DateTimeOffset>()))
                .Returns(message);

            ClassUnderTest.OnMessageReceived += (sender, args) => eventArgs.Add(args);
            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> m) =>
            {
                receivedMessages.Add(m);
                Thread.Sleep(150);
                return Task.CompletedTask;
            });
            var before = DateTimeOffset.UtcNow;
            await messageHandler(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None));
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
            Func<ProcessMessageEventArgs, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessageToReceive = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var serviceBusMessageToFilterOut = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var messageToReceive = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"}) { Environment = "unit-tests" };
            var messageToFilterOut = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"}) { Environment = "other-env"};
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<IServiceBusProcessor>().Object);
            GetMock<IServiceBusProcessor>()
                .Setup(x => x.RegisterMessageHandlerAsync(IsAny<Func<ProcessMessageEventArgs, Task>>(), IsAny<Func<ProcessErrorEventArgs, Task>>()))
                .Callback<Func<ProcessMessageEventArgs, Task>, Func<ProcessErrorEventArgs, Task>>((handler, _) => messageHandler = handler);
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
            await messageHandler(new ProcessMessageEventArgs(serviceBusMessageToReceive, null, CancellationToken.None));
            await messageHandler(new ProcessMessageEventArgs(serviceBusMessageToFilterOut, null, CancellationToken.None));

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(messageToReceive));
        }

        [Test]
        public async Task When_handling_a_message_timing_data_is_emitted_even_if_the_subscribed_handler_throws()
        {
            Func<ProcessMessageEventArgs, Task> messageHandler = null;
            var receivedMessages = new List<Message<TestData>>();
            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var message = new Message<TestData>(subscription.Topic, new TestData { Data = "test data"})
            {
                PublishedAt = DateTimeOffset.UtcNow.AddSeconds(-3),
                EnqueuedAt = DateTimeOffset.UtcNow.AddSeconds(-1)
            };
            var eventArgs = new List<MessageReceivedArgs>();
            var testException = new Exception("test exception");
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<IServiceBusProcessor>().Object);
            GetMock<IServiceBusProcessor>()
                .Setup(x => x.RegisterMessageHandlerAsync(IsAny<Func<ProcessMessageEventArgs, Task>>(), IsAny<Func<ProcessErrorEventArgs, Task>>()))
                .Callback<Func<ProcessMessageEventArgs, Task>, Func<ProcessErrorEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestData>(subscription.Topic, serviceBusMessage, IsAny<DateTimeOffset>()))
                .Returns(message);

            ClassUnderTest.OnMessageReceived += (sender, args) => eventArgs.Add(args);
            await ClassUnderTest.SubscribeAsync(subscription, async (Message<TestData> x) =>
            {
                receivedMessages.Add(x);
                await Task.Delay(150);
                throw testException;
            });
            var before = DateTimeOffset.UtcNow;
            var caught = Assert.CatchAsync(() => messageHandler(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None)));
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
            Func<ProcessErrorEventArgs, Task> exceptionHandler = null;
            var exceptionArgs = new List<ExceptionArgs>();
            var serviceBusExceptionArgs = new ProcessErrorEventArgs(new Exception("test exception"), ServiceBusErrorSource.Receive, "namespace", "entity path", CancellationToken.None);
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetClientAsync(subscription))
                .ReturnsAsync(GetMock<IServiceBusProcessor>().Object);
            GetMock<IServiceBusProcessor>()
                .Setup(x => x.RegisterMessageHandlerAsync(IsAny<Func<ProcessMessageEventArgs, Task>>(), IsAny<Func<ProcessErrorEventArgs, Task>>()))
                .Callback<Func<ProcessMessageEventArgs, Task>, Func<ProcessErrorEventArgs, Task>>((_, handler) => exceptionHandler = handler);

            ClassUnderTest.OnException += (sender, args) => exceptionArgs.Add(args);
            await ClassUnderTest.SubscribeAsync(subscription, (Message<TestData> message) => Task.CompletedTask);
            await exceptionHandler(serviceBusExceptionArgs);
            
            Assert.That(exceptionArgs.Count, Is.EqualTo(1));
            Assert.That(exceptionArgs[0].Subscription, Is.SameAs(subscription));
            Assert.That(exceptionArgs[0].Exception, Is.SameAs(serviceBusExceptionArgs.Exception));
            Assert.That(exceptionArgs[0].Action, Is.Empty);
            Assert.That(exceptionArgs[0].Endpoint, Is.Empty);
            Assert.That(exceptionArgs[0].EntityPath, Is.EqualTo(serviceBusExceptionArgs.EntityPath));
            Assert.That(exceptionArgs[0].ClientId, Is.Empty);
        }

        [Test]
        public async Task When_subscribing_to_the_dead_letter_queue()
        {
            Func<ProcessMessageEventArgs, Task> messageHandler = null;
            var testMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var expectedDeadLetter = new DeadLetter();
            DeadLetter deadLetter = null;
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetDeadLetterClientAsync(subscription))
                .ReturnsAsync(GetMock<IServiceBusProcessor>().Object);
            GetMock<IServiceBusProcessor>()
                .Setup(x => x.RegisterMessageHandlerAsync(IsAny<Func<ProcessMessageEventArgs, Task>>(), IsAny<Func<ProcessErrorEventArgs, Task>>()))
                .Callback<Func<ProcessMessageEventArgs, Task>, Func<ProcessErrorEventArgs, Task>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>().Setup(x => x.GetDeadLetter(testMessage)).Returns(expectedDeadLetter);

            await ClassUnderTest.SubscribeToDeadLettersAsync(subscription, x =>
            {
                deadLetter = x;
                return Task.CompletedTask;
            });
            await messageHandler(new ProcessMessageEventArgs(testMessage, null, CancellationToken.None));
            
            Assert.That(deadLetter, Is.EqualTo(expectedDeadLetter));
        }

        [Test]
        public void When_subscribing_to_a_single_topic_message_event()
        {
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<IServiceBusProcessor>().Object);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => {});

            GetMock<IServiceBusProcessor>().Verify(x => x.RegisterMessageHandlerAsync(ClassUnderTest.RouteMessage, IsAny<Func<ProcessErrorEventArgs, Task>>()));
        }

        [Test]
        public void When_subscribing_to_a_single_topic_message_event_multiple_times()
        {
            GetMock<IServiceBusProcessorPool>()
                .SetupSequence(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<IServiceBusProcessor>().Object)
                .Returns((IServiceBusProcessor)null);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => {});
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 2), (TestData message) => {});

            GetMock<IServiceBusProcessor>().Verify(x => x.RegisterMessageHandlerAsync(ClassUnderTest.RouteMessage, IsAny<Func<ProcessErrorEventArgs, Task>>()), Times.Once);
        }

        [Test]
        public async Task When_routing_a_message_it_should_only_reach_the_matching_subscriber()
        {
            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = "test-event.v1", };
            var deserializedMessage = new MessageEnvelope<TestData>() { Payload = new TestData { Data = "hello world" } };
            var testEventV1Messages = new List<TestData>();
            var testEventV2Messages = new List<TestData>();
            var otherEventV1Messages = new List<TestData>();
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<IServiceBusProcessor>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<TestData>(serviceBusMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 2), (TestData message) => testEventV1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("other-event", 1), (TestData message) => testEventV1Messages.Add(message));

            await ClassUnderTest.RouteMessage(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None));

            Assert.That(testEventV1Messages.Count, Is.EqualTo(1));
            Assert.That(testEventV1Messages[0], Is.SameAs(deserializedMessage.Payload));
            Assert.That(testEventV2Messages, Is.Empty);
            Assert.That(otherEventV1Messages, Is.Empty);
        }

        [Test]
        public async Task When_routing_a_message_and_there_are_multiple_subscribers()
        {
            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = "test-event.v3" };
            var deserializedMessage = new MessageEnvelope<TestData>() { Payload = new TestData { Data = "hello world" } };
            var subscriber1Messages = new List<TestData>();
            var subscriber2Messages = new List<TestData>();
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<IServiceBusProcessor>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<TestData>(serviceBusMessage)).Returns(deserializedMessage);

            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestData message) => subscriber1Messages.Add(message));
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 3), (TestData message) => subscriber2Messages.Add(message));

            await ClassUnderTest.RouteMessage(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None));

            Assert.That(subscriber1Messages.Count, Is.EqualTo(1), "Message missing from subscriber 1");
            Assert.That(subscriber1Messages[0], Is.SameAs(deserializedMessage.Payload));
            Assert.That(subscriber2Messages.Count, Is.EqualTo(1), "Message missing from subscriber 2");
            Assert.That(subscriber2Messages[0], Is.SameAs(deserializedMessage.Payload));
        }

        [Test]
        public async Task When_routing_a_message_you_get_timing_data()
        {
            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = "test-event.v3" };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<IServiceBusProcessor>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);

            ClassUnderTest.OnMessageReceived += (sender, args) => eventArgs.Add(args);
            var before = DateTimeOffset.UtcNow;
            await ClassUnderTest.RouteMessage(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None));
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
            await ClassUnderTest.RouteMessage(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None));

            Assert.That(eventArgs.Count, Is.EqualTo(2));
            Assert.That(eventArgs[1].ProcessingTime.TotalMilliseconds, Is.EqualTo(50).Within(20));
        }

        [Test]
        public void When_routing_a_message_you_get_timing_data_even_if_a_subscriber_blows_up()
        {
            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var messageEvent = new MessageEvent("test-event", 3);
            var deserializedObject = new MessageEnvelope<object> { MessageEvent = messageEvent.ToString() };
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<IServiceBusProcessor>().Object);
            GetMock<IMessageMapper>().Setup(x => x.ToMessageEnvelope<object>(serviceBusMessage)).Returns(deserializedObject);

            ClassUnderTest.Subscribe(messageEvent, (object _) =>
            {
                Thread.Sleep(50);
                throw new Exception("Test exception!");
            });

            ClassUnderTest.OnMessageReceived += (sender, args) => eventArgs.Add(args);
            var before = DateTimeOffset.UtcNow;
            Assert.CatchAsync<Exception>(() => ClassUnderTest.RouteMessage(new ProcessMessageEventArgs(serviceBusMessage, null, CancellationToken.None)));
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
            var serviceBusArgs = new ProcessErrorEventArgs(new Exception("test exception"),
                ServiceBusErrorSource.Complete,
                "namespace",
                "entity path",
                CancellationToken.None);
            var eventArgs = new List<ExceptionArgs>();
            Func<ProcessErrorEventArgs, Task> exceptionHandler = null;
            GetMock<IServiceBusProcessorPool>()
                .Setup(x => x.GetSingleTopicClientIfFirstTime())
                .Returns(GetMock<IServiceBusProcessor>().Object);
            GetMock<IServiceBusProcessor>()
                .Setup(x => x.RegisterMessageHandlerAsync(ClassUnderTest.RouteMessage, IsAny<Func<ProcessErrorEventArgs, Task>>()))
                .Callback<Func<ProcessMessageEventArgs, Task>, Func<ProcessErrorEventArgs, Task>>((_, handler) => exceptionHandler = handler);

            ClassUnderTest.OnException += (_, args) => eventArgs.Add(args);
            ClassUnderTest.Subscribe(new MessageEvent("test-event", 1), (TestData message) => {});
            exceptionHandler(serviceBusArgs);

            Assert.That(eventArgs.Count, Is.EqualTo(1));
            Assert.That(eventArgs[0].Subscription, Is.Null);
            Assert.That(eventArgs[0].Exception, Is.SameAs(serviceBusArgs.Exception));
            Assert.That(eventArgs[0].Action, Is.Empty);
            Assert.That(eventArgs[0].Endpoint, Is.Empty);
            Assert.That(eventArgs[0].EntityPath, Is.EqualTo("entity path"));
            Assert.That(eventArgs[0].ClientId, Is.Empty);
        }
    }
}