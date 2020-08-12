using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EL.ServiceBus.IntegrationTests
{
    public class Tests
    {
        private IMessagePublisher publisher;
        private IMessageSubscriber subscriber;
        private IMessageSerializer serializer;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            DependencyInjectionConfig.ConfigureServices(services);
            services.AddTransient<ISettings, Settings>();
            services.AddTransient(ctx => ctx.GetRequiredService<ISettings>().PublisherConfig);
            services.AddTransient(ctx => ctx.GetRequiredService<ISettings>().SubscriptionConfig);
            services.AddTransient<IMessageSerializer, MessageSerializer>();
            var serviceProvider = services.BuildServiceProvider();

            subscriber = serviceProvider.GetRequiredService<IMessageSubscriber>();
            publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            serializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        }

        [Test]
        public void RoundTripTests()
        {
            var topicA1 = new Topic("el-service-bus", "integration-test-a", 1);
            var topicA2 = new Topic("el-service-bus", "integration-test-a", 2);
            var topicB1 = new Topic("el-service-bus", "integration-test-b", 1);

            var subscriptionA1 = new Subscription(topicA1, "el-service-bus", "integration-tests");
            var subscriptionA2 = new Subscription(topicA2, "el-service-bus", "integration-tests");
            var subscriptionB1 = new Subscription(topicB1, "el-service-bus", "integration-tests");

            var messageRoundTripDurations = new List<double>();
            var receivedMessageCount = 0;
            var receivedA1Messages = new List<Message<string>>();
            var receivedA2Messages = new List<Message<int>>();
            var receivedB1Messages = new List<Message<IntegrationTestData>>();
            var exceptions = new List<Exception>();

            subscriber.OnServiceBusException += (_, args) => exceptions.Add(args.Exception);
            subscriber.OnMessageReceived += (_, args) =>
            {
                var duration = 0; //(args.ReceivedAt - args.EnqueuedAt).TotalMilliseconds;
                messageRoundTripDurations.Add(duration);
            };
            
            subscriber.Subscribe(subscriptionA1, (Message<string> message) =>
            {
                receivedA1Messages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe<int>(subscriptionA2, (Message<int> message) =>
            {
                receivedA2Messages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe(subscriptionB1, (Message<IntegrationTestData> message) =>
            {
                receivedB1Messages.Add(message);
                receivedMessageCount++;
            });

            var messagePublishDurations = new List<long>();
            var a1Message1 = new Message<string>(topicA1, $"hello-{Guid.NewGuid()}");
            var a1Message2 = new Message<string>(topicA1, $"world-{Guid.NewGuid()}");
            var a2Message1 = new Message<int>(topicA2, 13);
            var a2Message2 = new Message<int>(topicA2, 99);
            var a2Message3 = new Message<int>(topicA2, -123);
            var b1Message1 = new Message<IntegrationTestData>(topicB1, new IntegrationTestData { StringData = $"first-{Guid.NewGuid()}", IntData = 1 });
            var b1Message2 = new Message<IntegrationTestData>(topicB1, new IntegrationTestData { StringData = $"second-{Guid.NewGuid()}", IntData = -2 });

            publisher.OnMessagePublished += (object sender, MessagePublishedArgs args) =>
            {
                messagePublishDurations.Add(args.ElapsedMilliseconds);
            };

            publisher.Publish(a1Message1);
            publisher.Publish(a1Message2);
            publisher.Publish(a2Message1);
            publisher.Publish(a2Message2);
            publisher.Publish(a2Message3);
            publisher.Publish(b1Message1);
            publisher.Publish(b1Message2);

            var waited = 0;
            var expectedMessageCount = 7;
            while (receivedMessageCount < expectedMessageCount && waited < 5000)
            {
                Thread.Sleep(100);
                waited += 100;
            }
            Console.WriteLine($"Waited for {waited}ms");

            Assert.That(exceptions.Count, Is.EqualTo(0), "Got unexpected exceptions!");
            exceptions.ForEach(x => Console.WriteLine($"{x.Message}"));

            AssertAllMessagesMatchTopic(receivedA1Messages, topicA1);
            AssertAllMessagesMatchTopic(receivedA2Messages, topicA2);
            AssertAllMessagesMatchTopic(receivedB1Messages, topicB1);

            AssertTestMessageReceived(receivedA1Messages, a1Message1);
            AssertTestMessageReceived(receivedA1Messages, a1Message2);
            AssertTestMessageReceived(receivedA2Messages, a2Message1);
            AssertTestMessageReceived(receivedA2Messages, a2Message2);
            AssertTestMessageReceived(receivedA2Messages, a2Message3);
            AssertTestMessageReceived(receivedB1Messages, b1Message1);
            AssertTestMessageReceived(receivedB1Messages, b1Message2);

            Assert.That(receivedMessageCount, Is.GreaterThanOrEqualTo(expectedMessageCount), $"Did not get the expected number of messages");
            Assert.That(messageRoundTripDurations.Count, Is.GreaterThanOrEqualTo(receivedMessageCount), "Did not get the expected number of round trip durations");
            Assert.That(messageRoundTripDurations.Max(), Is.LessThanOrEqualTo(2000), $"Expected round trip durations to be < 2000ms");
            Assert.That(messageRoundTripDurations.Min(), Is.GreaterThanOrEqualTo(0), $"Expected round trip durations to be >= 0ms");
            Assert.That(messagePublishDurations.Count, Is.EqualTo(expectedMessageCount), "Did not get the expected number of publish durations");
            Assert.That(messagePublishDurations.Average(), Is.LessThanOrEqualTo(500), $"Expected publish durations to be < 500ms");
        }

        private void AssertTestMessageReceived<T>(List<Message<T>> receivedMessages, Message<T> expectedMessage)
        {
            var match = receivedMessages.FirstOrDefault(x => x.MessageId == expectedMessage.MessageId);
            Assert.That(match, Is.Not.Null, $"Unable to find matching message on topic {expectedMessage.Topic}");
            Assert.That(serializer.Serialize(match), Is.EqualTo(serializer.Serialize(expectedMessage)));
        }

        private void AssertAllMessagesMatchTopic<T>(List<Message<T>> receivedMessages, Topic expectedTopic)
        {
            var mismatches = receivedMessages.Where(x => x.Topic.ToString() != expectedTopic.ToString()).ToList();
            if (mismatches.Any())
            {
                Assert.Fail($"Expected {expectedTopic} but found {string.Join(", ", mismatches)}");
            }
        }
    }

    public class IntegrationTestData
    {
        public string StringData { get; set; }
        public int IntData { get; set; }
    }
}