using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using El.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace EL.ServiceBus.IntegrationTests
{
    public class Tests
    {
        [Test]
        public void RoundTripTests()
        {
            var builder = new HostBuilder()
                .ConfigureELMessaging()
                .ConfigureServices((HostBuilderContext, services) => {
                    DependencyInjectionConfig.ConfigureServices(services);
                    services.AddTransient<INameResolver, Settings>();
                    services.AddTransient<ISettings, Settings>();
                    services.AddTransient<ITopicConfig, TestTopicConfig>();
                    services.AddTransient<IMessageSerializer, MessageSerializer>();
                });
            using (var host = builder.Build())
            {
                var serviceProvider = host.Services;
                var subscriber = serviceProvider.GetRequiredService<IMessageSubscriber>();
                var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

                var hostedTask = host.RunAsync();

                var receivedMessageCount = 0;
                var receivedA1Messages = new List<IntegrationTestMessage>();
                var receivedA2Messages = new List<IntegrationTestMessage>();
                var receivedB1Messages = new List<IntegrationTestMessage>();
                
                subscriber.Subscribe("event-a", 1, (IntegrationTestMessage message) => {
                    receivedA1Messages.Add(message);
                    receivedMessageCount++;
                });
                subscriber.Subscribe("event-a", 2, (IntegrationTestMessage message) => {
                    receivedA2Messages.Add(message);
                    receivedMessageCount++;
                });
                subscriber.Subscribe("event-b", 1, (IntegrationTestMessage message) => {
                    receivedB1Messages.Add(message);
                    receivedMessageCount++;
                });

                var a11Message = new IntegrationTestMessage { StringData = "A1-1", IntData = 13 };
                var a12Message = new IntegrationTestMessage { StringData = "A1-2", IntData = 99 };
                var a21Message = new IntegrationTestMessage { StringData = "A2-1", IntData = 7 };
                var b11Message = new IntegrationTestMessage { StringData = "B1-1", IntData = -123 };

                publisher.Publish("event-a", 1, a11Message);
                publisher.Publish("event-a", 2, a21Message);
                publisher.Publish("event-b", 1, b11Message);
                publisher.Publish("event-a", 1, a12Message);

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
            }
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