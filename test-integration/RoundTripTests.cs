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

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            DependencyInjectionConfig.ConfigureServices(services);
            services.AddTransient<ISettings, Settings>();
            services.AddTransient(ctx => ctx.GetRequiredService<ISettings>().TopicConfig);
            services.AddTransient(ctx => ctx.GetRequiredService<ISettings>().SubscriptionConfig);
            services.AddTransient<IMessageSerializer, MessageSerializer>();
            var serviceProvider = services.BuildServiceProvider();

            subscriber = serviceProvider.GetRequiredService<IMessageSubscriber>();
            publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        }

        [Test]
        public void RoundTripTests()
        {
            var eventA1 = new MessageEvent("event-a", 1);
            var eventA2 = new MessageEvent("event-a", 2);
            var eventB1 = new MessageEvent("event-b", 1);
            var stringEvent = new MessageEvent("string-event", 1);
            var intEvent = new MessageEvent("int-event", 1);

            var messageRoundTripDurations = new List<double>();
            var receivedMessageCount = 0;
            var receivedA1Messages = new List<IntegrationTestMessage>();
            var receivedA2Messages = new List<IntegrationTestMessage>();
            var receivedB1Messages = new List<IntegrationTestMessage>();
            var receivedStringMessages = new List<string>();
            var receivedIntMessages = new List<int>();
            var exceptions = new List<Exception>();

            subscriber.OnUnhandledException += (_, args) => exceptions.Add(args.UnhandledException);
            subscriber.OnServiceBusException += (_, args) => exceptions.Add(args.Exception);

            subscriber.OnMessageReceived += (object sender, MessageReceivedArgs args) =>
            {
                var duration = (args.ReceivedAt - args.PublishedAt).TotalMilliseconds;
                messageRoundTripDurations.Add(duration);
            };
            subscriber.Subscribe(eventA1, (IntegrationTestMessage message) =>
            {
                receivedA1Messages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe(eventA2, (IntegrationTestMessage message) =>
            {
                receivedA2Messages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe(eventB1, (IntegrationTestMessage message) =>
            {
                receivedB1Messages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe(stringEvent, (string message) =>
            {
                receivedStringMessages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe(intEvent, (int message) =>
            {
                receivedIntMessages.Add(message);
                receivedMessageCount++;
            });

            var messagePublishDurations = new List<long>();
            var a11Message = new IntegrationTestMessage { StringData = "A1-1", IntData = 13 };
            var a12Message = new IntegrationTestMessage { StringData = "A1-2", IntData = 99 };
            var a21Message = new IntegrationTestMessage { StringData = "A2-1", IntData = 7 };
            var b11Message = new IntegrationTestMessage { StringData = "B1-1", IntData = -123 };

            publisher.OnMessagePublished += (object sender, MessagePublishedArgs args) =>
            {
                messagePublishDurations.Add(args.ElapsedMilliseconds);
            };

            publisher.Publish(eventA1, a11Message);
            publisher.Publish(eventA2, a21Message);
            publisher.Publish(eventB1, b11Message);
            publisher.Publish(eventA1, a12Message);
            publisher.Publish(stringEvent, "first");
            publisher.Publish(intEvent, 1);
            publisher.Publish(stringEvent, "second");
            publisher.Publish(intEvent, 2);

            var waited = 0;
            var expectedMessageCount = 8;
            while (receivedMessageCount < expectedMessageCount && waited < 5000)
            {
                Thread.Sleep(100);
                waited += 100;
            }
            Console.WriteLine($"Waited for {waited}ms");

            Assert.That(exceptions.Count, Is.EqualTo(0), "Got unexpected exceptions!");

            AssertTestMessageReceived(receivedA1Messages, a11Message);
            AssertTestMessageReceived(receivedA1Messages, a12Message);
            AssertTestMessageReceived(receivedA2Messages, a21Message);
            AssertTestMessageReceived(receivedB1Messages, b11Message);
            Assert.That(receivedStringMessages, Does.Contain("first"));
            Assert.That(receivedStringMessages, Does.Contain("second"));
            Assert.That(receivedIntMessages, Does.Contain(1));
            Assert.That(receivedIntMessages, Does.Contain(2));
            Assert.That(receivedMessageCount, Is.GreaterThanOrEqualTo(expectedMessageCount), $"Did not get the expected number of messages");
            Assert.That(messageRoundTripDurations.Count, Is.GreaterThanOrEqualTo(receivedMessageCount), "Did not get the expected number of round trip durations");
            Assert.That(messageRoundTripDurations.Max(), Is.LessThanOrEqualTo(2000), $"Expected round trip durations to be < 2000ms");
            Assert.That(messageRoundTripDurations.Min(), Is.GreaterThanOrEqualTo(0), $"Expected round trip durations to be >= 0ms");
            Assert.That(messagePublishDurations.Count, Is.EqualTo(expectedMessageCount), "Did not get the expected number of publish durations");
            Assert.That(messagePublishDurations.Average(), Is.LessThanOrEqualTo(500), $"Expected publish durations to be < 500ms");
        }

        private void AssertTestMessageReceived(List<IntegrationTestMessage> messages, IntegrationTestMessage expected)
        {
            Assert.That(messages.Any(x => x.StringData == expected.StringData && x.IntData == expected.IntData), $"Message {expected.StringData} not found");
        }
    }

    public class IntegrationTestMessage
    {
        public string StringData { get; set; }
        public int IntData { get; set; }
    }
}