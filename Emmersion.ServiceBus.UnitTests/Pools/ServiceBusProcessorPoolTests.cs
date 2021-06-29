using System.Threading.Tasks;
using Emmersion.ServiceBus.Pools;
using Emmersion.ServiceBus.SdkWrappers;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests.Pools
{
    internal class ServiceBusProcessorPoolTests : With_an_automocked<ServiceBusProcessorPool>
    {
        private Subscription subscription = new Subscription(new Topic("el-service-bus", "test-event", 1), "el-service-bus", "listener");
        private Subscription otherSubscription = new Subscription(new Topic("el-service-bus", "other-test-event", 2), "el-service-bus", "listener");

        [Test]
        public async Task When_getting_client_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);

            var result = await ClassUnderTest.GetClientAsync(subscription);

            Assert.That(result, Is.SameAs(mockSubscriptionClientWrapper.Object));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(subscription));
        }

        [Test]
        public async Task When_getting_client_after_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);

            await ClassUnderTest.GetClientAsync(subscription);
            var result = Assert.CatchAsync(() => ClassUnderTest.GetClientAsync(subscription));

            Assert.That(result.Message, Does.Contain(subscription.ToString()));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(IsAny<Subscription>()), Times.Once);
        }

        [Test]
        public async Task When_getting_different_clients()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);
            var otherMockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.Create(otherSubscription))
                .Returns(otherMockSubscriptionClientWrapper.Object);

            var result1 = await ClassUnderTest.GetClientAsync(subscription);
            var result2 = await ClassUnderTest.GetClientAsync(otherSubscription);

            Assert.That(result1, Is.SameAs(mockSubscriptionClientWrapper.Object));
            Assert.That(result2, Is.SameAs(otherMockSubscriptionClientWrapper.Object));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(subscription));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(otherSubscription));
        }

        [Test]
        public async Task When_getting_a_dead_letter_client_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.CreateDeadLetter(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);

            var result = await ClassUnderTest.GetDeadLetterClientAsync(subscription);

            Assert.That(result, Is.SameAs(mockSubscriptionClientWrapper.Object));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(subscription));
        }

        [Test]
        public async Task When_getting_a_dead_letter_client_after_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.CreateDeadLetter(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);

            await ClassUnderTest.GetDeadLetterClientAsync(subscription);
            var result = Assert.CatchAsync(() => ClassUnderTest.GetDeadLetterClientAsync(subscription));

            Assert.That(result.Message, Does.Contain(subscription.ToString()));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(IsAny<Subscription>()), Times.Once);
        }

        [Test]
        public void When_getting_the_single_topic_client_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.CreateSingleTopic())
                .Returns(mockSubscriptionClientWrapper.Object);

            var result = ClassUnderTest.GetSingleTopicClientIfFirstTime();

            Assert.That(result, Is.SameAs(mockSubscriptionClientWrapper.Object));
        }

        [Test]
        public void When_getting_the_single_topic_client_after_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.CreateSingleTopic())
                .Returns(mockSubscriptionClientWrapper.Object);

            var result1 = ClassUnderTest.GetSingleTopicClientIfFirstTime();
            var result2 = ClassUnderTest.GetSingleTopicClientIfFirstTime();

            Assert.That(result1, Is.SameAs(mockSubscriptionClientWrapper.Object));
            Assert.That(result2, Is.Null);
        }

        [Test]
        public async Task When_disposing_and_there_are_subscriptions()
        {
            var mockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);
            var otherMockSubscriptionClientWrapper = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusProcessorCreator>()
                .Setup(x => x.Create(otherSubscription))
                .Returns(otherMockSubscriptionClientWrapper.Object);

            await ClassUnderTest.GetClientAsync(subscription);
            await ClassUnderTest.GetClientAsync(otherSubscription);
            await ClassUnderTest.DisposeAsync();

            mockSubscriptionClientWrapper.Verify(x => x.CloseAsync());
            otherMockSubscriptionClientWrapper.Verify(x => x.CloseAsync());
        }

        [Test]
        public async Task When_disposing_and_there_are_no_subscriptions()
        {
            await ClassUnderTest.DisposeAsync();
        }
    }
}