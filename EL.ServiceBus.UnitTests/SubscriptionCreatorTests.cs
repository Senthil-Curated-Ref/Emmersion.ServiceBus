using System;
using System.Threading.Tasks;
using Emmersion.Testing;
using Microsoft.Azure.ServiceBus.Management;
using Moq;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class SubscriptionCreatorTests : With_an_automocked<SubscriptionCreator>
    {
        private Subscription subscription = new Subscription(new Topic("el-service-bus", "test-event", 1), "unit-test", "listener");

        [SetUp]
        public void SetUp()
        {
            GetMock<IManagementClientWrapperPool>()
                .Setup(x => x.GetClient())
                .Returns(GetMock<IManagementClientWrapper>().Object);
        }

        [Test]
        public async Task When_creating_a_subscription_and_it_already_exists()
        {
            GetMock<IManagementClientWrapper>()
                .Setup(x => x.DoesSubscriptionExist(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(true);
            
            await ClassUnderTest.CreateSubscriptionIfNecessary(subscription);

            GetMock<IManagementClientWrapper>().VerifyNever(x => x.CreateSubscription(IsAny<SubscriptionDescription>()));
        }

        [Test]
        public void When_creating_a_subscription_and_the_topic_does_not_exist()
        {
            GetMock<IManagementClientWrapper>()
                .Setup(x => x.DoesSubscriptionExist(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(false);
            GetMock<IManagementClientWrapper>()
                .Setup(x => x.DoesTopicExist(subscription.Topic.ToString()))
                .ReturnsAsync(false);
            
            var exception = Assert.CatchAsync(() => ClassUnderTest.CreateSubscriptionIfNecessary(subscription));

            Assert.That(exception.Message.Contains($"Topic {subscription.Topic} does not exist"));
            GetMock<IManagementClientWrapper>().VerifyNever(x => x.CreateSubscription(IsAny<SubscriptionDescription>()));
        }

        [Test]
        public async Task When_creating_a_subscription_successfully()
        {
            SubscriptionDescription description = null;
            GetMock<IManagementClientWrapper>()
                .Setup(x => x.DoesSubscriptionExist(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(false);
            GetMock<IManagementClientWrapper>()
                .Setup(x => x.DoesTopicExist(subscription.Topic.ToString()))
                .ReturnsAsync(true);
                GetMock<IManagementClientWrapper>()
                    .Setup(x => x.CreateSubscription(IsAny<SubscriptionDescription>()))
                    .Callback<SubscriptionDescription>(x => description = x);
            
            await ClassUnderTest.CreateSubscriptionIfNecessary(subscription);

            GetMock<IManagementClientWrapper>().Verify(x => x.CreateSubscription(description));
            Assert.That(description.TopicPath, Is.EqualTo(subscription.Topic.ToString()));
            Assert.That(description.SubscriptionName, Is.EqualTo(subscription.SubscriptionName));
            Assert.That(description.MaxDeliveryCount, Is.EqualTo(10));
            Assert.That(description.AutoDeleteOnIdle, Is.EqualTo(TimeSpan.MaxValue));
            Assert.That(description.DefaultMessageTimeToLive, Is.EqualTo(TimeSpan.FromDays(14)));
            Assert.That(description.EnableDeadLetteringOnFilterEvaluationExceptions, Is.True);
            Assert.That(description.EnableDeadLetteringOnMessageExpiration, Is.True);
            Assert.That(description.LockDuration, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public async Task When_creating_a_subscription_that_has_auto_delete_in_the_name()
        {
            subscription = new Subscription(subscription.Topic, "unit-tests", "auto-delete-soon");
            SubscriptionDescription description = null;
            GetMock<IManagementClientWrapper>()
                .Setup(x => x.DoesSubscriptionExist(subscription.Topic.ToString(), subscription.SubscriptionName))
                .ReturnsAsync(false);
            GetMock<IManagementClientWrapper>()
                .Setup(x => x.DoesTopicExist(subscription.Topic.ToString()))
                .ReturnsAsync(true);
                GetMock<IManagementClientWrapper>()
                    .Setup(x => x.CreateSubscription(IsAny<SubscriptionDescription>()))
                    .Callback<SubscriptionDescription>(x => description = x);
            
            await ClassUnderTest.CreateSubscriptionIfNecessary(subscription);

            GetMock<IManagementClientWrapper>().Verify(x => x.CreateSubscription(description));
            Assert.That(description.TopicPath, Is.EqualTo(subscription.Topic.ToString()));
            Assert.That(description.SubscriptionName, Is.EqualTo(subscription.SubscriptionName));
            Assert.That(description.MaxDeliveryCount, Is.EqualTo(10));
            Assert.That(description.AutoDeleteOnIdle, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(description.DefaultMessageTimeToLive, Is.EqualTo(TimeSpan.FromDays(14)));
            Assert.That(description.EnableDeadLetteringOnFilterEvaluationExceptions, Is.True);
            Assert.That(description.EnableDeadLetteringOnMessageExpiration, Is.True);
            Assert.That(description.LockDuration, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }
    }
}