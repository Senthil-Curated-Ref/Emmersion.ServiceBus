using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using Emmersion.ServiceBus.SdkWrappers;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests
{
    internal class SubscriptionCreatorTests : With_an_automocked<SubscriptionCreator>
    {
        private Subscription subscription = new Subscription(new Topic("el-service-bus", "test-event", 1), "unit-test", "listener");

        [SetUp]
        public void SetUp()
        {
            GetMock<IManagementClientWrapperPool>()
                .Setup(x => x.GetClient())
                .Returns(GetMock<IServiceBusAdministrationClient>().Object);
        }

        [Test]
        public async Task When_creating_a_subscription_and_it_already_exists()
        {
            GetMock<IServiceBusAdministrationClient>()
                .Setup(x => x.DoesSubscriptionExistAsync(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(true);
            
            await ClassUnderTest.CreateSubscriptionIfNecessaryAsync(subscription);

            GetMock<IServiceBusAdministrationClient>().VerifyNever(x => x.CreateSubscriptionAsync(IsAny<CreateSubscriptionOptions>()));
        }

        [Test]
        public void When_creating_a_subscription_and_the_topic_does_not_exist()
        {
            GetMock<IServiceBusAdministrationClient>()
                .Setup(x => x.DoesSubscriptionExistAsync(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(false);
            GetMock<IServiceBusAdministrationClient>()
                .Setup(x => x.DoesTopicExistAsync(subscription.Topic.ToString()))
                .ReturnsAsync(false);
            
            var exception = Assert.CatchAsync(() => ClassUnderTest.CreateSubscriptionIfNecessaryAsync(subscription));

            Assert.That(exception.Message.Contains($"Topic {subscription.Topic} does not exist"));
            GetMock<IServiceBusAdministrationClient>().VerifyNever(x => x.CreateSubscriptionAsync(IsAny<CreateSubscriptionOptions>()));
        }

        [Test]
        public async Task When_creating_a_subscription_successfully()
        {
            CreateSubscriptionOptions options = null;
            GetMock<IServiceBusAdministrationClient>()
                .Setup(x => x.DoesSubscriptionExistAsync(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(false);
            GetMock<IServiceBusAdministrationClient>()
                .Setup(x => x.DoesTopicExistAsync(subscription.Topic.ToString()))
                .ReturnsAsync(true);
                GetMock<IServiceBusAdministrationClient>()
                    .Setup(x => x.CreateSubscriptionAsync(IsAny<CreateSubscriptionOptions>()))
                    .Callback<CreateSubscriptionOptions>(x => options = x);
            
            await ClassUnderTest.CreateSubscriptionIfNecessaryAsync(subscription);

            GetMock<IServiceBusAdministrationClient>().Verify(x => x.CreateSubscriptionAsync(options));
            Assert.That(options.TopicName, Is.EqualTo(subscription.Topic.ToString()));
            Assert.That(options.SubscriptionName, Is.EqualTo(subscription.SubscriptionName));
            Assert.That(options.MaxDeliveryCount, Is.EqualTo(10));
            Assert.That(options.AutoDeleteOnIdle, Is.EqualTo(TimeSpan.MaxValue));
            Assert.That(options.DefaultMessageTimeToLive, Is.EqualTo(TimeSpan.FromDays(14)));
            Assert.That(options.EnableDeadLetteringOnFilterEvaluationExceptions, Is.True);
            Assert.That(options.DeadLetteringOnMessageExpiration, Is.True);
            Assert.That(options.LockDuration, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public async Task When_creating_a_subscription_that_has_auto_delete_in_the_name()
        {
            subscription = new Subscription(subscription.Topic, "unit-tests", "auto-delete-soon");
            CreateSubscriptionOptions options = null;
            GetMock<IServiceBusAdministrationClient>()
                .Setup(x => x.DoesSubscriptionExistAsync(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(false);
            GetMock<IServiceBusAdministrationClient>()
                .Setup(x => x.DoesTopicExistAsync(subscription.Topic.ToString()))
                .ReturnsAsync(true);
                GetMock<IServiceBusAdministrationClient>()
                    .Setup(x => x.CreateSubscriptionAsync(IsAny<CreateSubscriptionOptions>()))
                    .Callback<CreateSubscriptionOptions>(x => options = x);
            
            await ClassUnderTest.CreateSubscriptionIfNecessaryAsync(subscription);

            GetMock<IServiceBusAdministrationClient>().Verify(x => x.CreateSubscriptionAsync(options));
            Assert.That(options.TopicName, Is.EqualTo(subscription.Topic.ToString()));
            Assert.That(options.SubscriptionName, Is.EqualTo(subscription.SubscriptionName));
            Assert.That(options.MaxDeliveryCount, Is.EqualTo(10));
            Assert.That(options.AutoDeleteOnIdle, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(options.DefaultMessageTimeToLive, Is.EqualTo(TimeSpan.FromDays(14)));
            Assert.That(options.EnableDeadLetteringOnFilterEvaluationExceptions, Is.True);
            Assert.That(options.DeadLetteringOnMessageExpiration, Is.True);
            Assert.That(options.LockDuration, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }
    }
}