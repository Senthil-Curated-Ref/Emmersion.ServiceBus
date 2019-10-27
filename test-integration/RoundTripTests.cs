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

            var messageRoundTripDurations = new List<double>();
            var receivedMessageCount = 0;
            var receivedA1Messages = new List<IntegrationTestMessage>();
            var receivedA2Messages = new List<IntegrationTestMessage>();
            var receivedB1Messages = new List<IntegrationTestMessage>();

            subscriber.OnMessageReceived += (object sender, MessageReceivedArgs args) => {
                var duration = (args.ReceivedAt - args.PublishedAt).TotalMilliseconds;
                messageRoundTripDurations.Add(duration);
            };
            subscriber.Subscribe(eventA1, (IntegrationTestMessage message) => {
                receivedA1Messages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe(eventA2, (IntegrationTestMessage message) => {
                receivedA2Messages.Add(message);
                receivedMessageCount++;
            });
            subscriber.Subscribe(eventB1, (IntegrationTestMessage message) => {
                receivedB1Messages.Add(message);
                receivedMessageCount++;
            });

            var messagePublishDurations = new List<long>();
            var a11Message = new IntegrationTestMessage { StringData = "A1-1", IntData = 13 };
            var a12Message = new IntegrationTestMessage { StringData = "A1-2", IntData = 99 };
            var a21Message = new IntegrationTestMessage { StringData = "A2-1", IntData = 7 };
            var b11Message = new IntegrationTestMessage { StringData = "B1-1", IntData = -123 };

            publisher.OnMessagePublished += (object sender, MessagePublishedArgs args) => {
                messagePublishDurations.Add(args.ElapsedMilliseconds);
            };
            publisher.Publish(eventA1, a11Message);
            publisher.Publish(eventA2, a21Message);
            publisher.Publish(eventB1, b11Message);
            publisher.Publish(eventA1, a12Message);

            var waited = 0;
            var expectedMessageCount = 4;
            while (receivedMessageCount < expectedMessageCount && waited < 5000)
            {
                Thread.Sleep(100);
                waited += 100;
            }
            Console.WriteLine($"Waited for {waited}ms");

            AssertMessageReceived(receivedA1Messages, a11Message);
            AssertMessageReceived(receivedA1Messages, a12Message);
            AssertMessageReceived(receivedA2Messages, a21Message);
            AssertMessageReceived(receivedB1Messages, b11Message);
            Assert.That(receivedMessageCount, Is.GreaterThanOrEqualTo(expectedMessageCount), $"Did not get the expected number of messages");
            Assert.That(messageRoundTripDurations.Count, Is.GreaterThanOrEqualTo(receivedMessageCount), "Did not get the expected number of round trip durations");
            Assert.That(messageRoundTripDurations.Max(), Is.LessThanOrEqualTo(2000), $"Expected round trip durations to be < 2000ms");
            Assert.That(messageRoundTripDurations.Min(), Is.GreaterThanOrEqualTo(0), $"Expected round trip durations to be >= 0ms");
            Assert.That(messagePublishDurations.Count, Is.EqualTo(expectedMessageCount), "Did not get the expected number of publish durations");
            Assert.That(messagePublishDurations.Average(), Is.LessThanOrEqualTo(500), $"Expected publish durations to be < 500ms");
        }

        private void AssertMessageReceived(List<IntegrationTestMessage> messages, IntegrationTestMessage expected)
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