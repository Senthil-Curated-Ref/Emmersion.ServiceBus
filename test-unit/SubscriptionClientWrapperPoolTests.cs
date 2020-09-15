using Moq;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class SubscriptionClientWrapperPoolTests : With_an_automocked<SubscriptionClientWrapperPool>
    {
        private Subscription subscription = new Subscription(new Topic("el-service-bus", "test-event", 1), "el-service-bus", "listener");
        private Subscription otherSubscription = new Subscription(new Topic("el-service-bus", "other-test-event", 2), "el-service-bus", "listener");

        [Test]
        public void When_getting_client_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);

            var result = ClassUnderTest.GetClient(subscription);

            Assert.That(result, Is.SameAs(mockSubscriptionClientWrapper.Object));
        }

        [Test]
        public void When_getting_client_after_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);

            ClassUnderTest.GetClient(subscription);
            var result = Assert.Catch(() => ClassUnderTest.GetClient(subscription));

            Assert.That(result.Message, Does.Contain(subscription.ToString()));
        }

        [Test]
        public void When_getting_different_clients()
        {
            var mockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);
            var otherMockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(otherSubscription))
                .Returns(otherMockSubscriptionClientWrapper.Object);

            var result1 = ClassUnderTest.GetClient(subscription);
            var result2 = ClassUnderTest.GetClient(otherSubscription);

            Assert.That(result1, Is.SameAs(mockSubscriptionClientWrapper.Object));
            Assert.That(result2, Is.SameAs(otherMockSubscriptionClientWrapper.Object));
        }

        [Test]
        public void When_getting_the_single_topic_client_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.CreateSingleTopic())
                .Returns(mockSubscriptionClientWrapper.Object);

            var result = ClassUnderTest.GetSingleTopicClientIfFirstTime();

            Assert.That(result, Is.SameAs(mockSubscriptionClientWrapper.Object));
        }

        [Test]
        public void When_getting_the_single_topic_client_after_the_first_time()
        {
            var mockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.CreateSingleTopic())
                .Returns(mockSubscriptionClientWrapper.Object);

            var result1 = ClassUnderTest.GetSingleTopicClientIfFirstTime();
            var result2 = ClassUnderTest.GetSingleTopicClientIfFirstTime();

            Assert.That(result1, Is.SameAs(mockSubscriptionClientWrapper.Object));
            Assert.That(result2, Is.Null);
        }

        [Test]
        public void When_disposing_and_there_are_subscriptions()
        {
            var mockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(subscription))
                .Returns(mockSubscriptionClientWrapper.Object);
            var otherMockSubscriptionClientWrapper = new Mock<ISubscriptionClientWrapper>();
            GetMock<ISubscriptionClientWrapperCreator>()
                .Setup(x => x.Create(otherSubscription))
                .Returns(otherMockSubscriptionClientWrapper.Object);

            ClassUnderTest.GetClient(subscription);
            ClassUnderTest.GetClient(otherSubscription);
            ClassUnderTest.Dispose();

            mockSubscriptionClientWrapper.Verify(x => x.CloseAsync());
            otherMockSubscriptionClientWrapper.Verify(x => x.CloseAsync());
        }

        [Test]
        public void When_disposing_and_there_are_no_subscriptions()
        {
            Assert.DoesNotThrow(() => ClassUnderTest.Dispose());
        }
    }
}