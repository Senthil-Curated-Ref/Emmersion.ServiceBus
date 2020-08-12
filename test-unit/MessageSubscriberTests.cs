using System;
using System.Collections.Generic;
using System.Threading;
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
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(Any<Action<Microsoft.Azure.ServiceBus.Message>>(), Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestMessage>(subscription.Topic, serviceBusMessage))
                .Returns(message);

            ClassUnderTest.Subscribe(subscription, (Message<TestMessage> message) => receivedMessages.Add(message));
            messageHandler(serviceBusMessage);

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0], Is.SameAs(message));
        }

        [Test]
        public void When_handling_a_message_timing_data_is_emitted()
        {
            Action<Microsoft.Azure.ServiceBus.Message> messageHandler = null;
            var receivedMessages = new List<Message<TestMessage>>();
            var publishedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message {
                ScheduledEnqueueTimeUtc = publishedAt.UtcDateTime
            };
            var message = new Message<TestMessage>(subscription.Topic, new TestMessage { Data = "test data"});
            var eventArgs = new List<MessageReceivedArgs>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(Any<Action<Microsoft.Azure.ServiceBus.Message>>(), Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestMessage>(subscription.Topic, serviceBusMessage))
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
            Assert.That(eventArgs[0].EnqueuedAt, Is.EqualTo(publishedAt));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(eventArgs[0].ReceivedAt, Is.EqualTo(before).Within(TimeSpan.FromMilliseconds(10)));
            Assert.That(eventArgs[0].ProcessingTime, Is.EqualTo(duration).Within(TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public void When_handling_a_message_timing_data_is_emitted_even_if_the_subscribed_action_throws()
        {
            Action<Microsoft.Azure.ServiceBus.Message> messageHandler = null;
            var receivedMessages = new List<Message<TestMessage>>();
            var publishedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var serviceBusMessage = new Microsoft.Azure.ServiceBus.Message {
                ScheduledEnqueueTimeUtc = publishedAt.UtcDateTime
            };
            var message = new Message<TestMessage>(subscription.Topic, new TestMessage { Data = "test data"});
            var eventArgs = new List<MessageReceivedArgs>();
            var testException = new Exception("test exception");
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(GetMock<ISubscriptionClientWrapper>().Object);
            GetMock<ISubscriptionClientWrapper>()
                .Setup(x => x.RegisterMessageHandler(Any<Action<Microsoft.Azure.ServiceBus.Message>>(), Any<Action<ExceptionReceivedEventArgs>>()))
                .Callback<Action<Microsoft.Azure.ServiceBus.Message>, Action<ExceptionReceivedEventArgs>>((handler, _) => messageHandler = handler);
            GetMock<IMessageMapper>()
                .Setup(x => x.FromServiceBusMessage<TestMessage>(subscription.Topic, serviceBusMessage))
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
            Assert.That(eventArgs[0].EnqueuedAt, Is.EqualTo(publishedAt));
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
    }
}