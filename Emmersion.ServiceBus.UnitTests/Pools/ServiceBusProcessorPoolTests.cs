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
        private string connectionString = "connection-string";
        private string singleTopicConnectionString = "single-topic-connection-string";
        private string singleTopicName = "single-topic-name";
        private string singleTopicSubscriptionName = "single-topic-subscription-name";
        private int maxConcurrentCalls = 13;
        private Mock<IServiceBusClient> mockServiceBusClient;

        [SetUp]
        public void SetUp()
        {
            mockServiceBusClient = new Mock<IServiceBusClient>();
            
            GetMock<ISubscriptionConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ISubscriptionConfig>().Setup(x => x.SingleTopicConnectionString).Returns(singleTopicConnectionString);
            GetMock<ISubscriptionConfig>().Setup(x => x.SingleTopicName).Returns(singleTopicName);
            GetMock<ISubscriptionConfig>().Setup(x => x.SingleTopicSubscriptionName).Returns(singleTopicSubscriptionName);
            GetMock<ISubscriptionConfig>().Setup(x => x.MaxConcurrentMessages).Returns(maxConcurrentCalls);
        }

        [Test]
        public async Task When_getting_client_the_first_time()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClientAsync(connectionString)).ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName, maxConcurrentCalls))
                .Returns(mockProcessor.Object);

            var result = await ClassUnderTest.GetProcessorAsync(subscription);

            Assert.That(result, Is.SameAs(mockProcessor.Object));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(subscription));
        }

        [Test]
        public async Task When_getting_client_after_the_first_time()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClientAsync(connectionString)).ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName, maxConcurrentCalls))
                .Returns(mockProcessor.Object);

            var first = await ClassUnderTest.GetProcessorAsync(subscription);
            var result = Assert.CatchAsync(() => ClassUnderTest.GetProcessorAsync(subscription));

            Assert.That(first, Is.SameAs(mockProcessor.Object));
            Assert.That(result.Message, Does.Contain(subscription.ToString()));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(IsAny<Subscription>()), Times.Once);
        }

        [Test]
        public async Task When_getting_different_clients()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClientAsync(connectionString)).ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName, maxConcurrentCalls))
                .Returns(mockProcessor.Object);
            var otherMockProcessor = new Mock<IServiceBusProcessor>();
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(otherSubscription.Topic.ToString(), otherSubscription.SubscriptionName, maxConcurrentCalls))
                .Returns(otherMockProcessor.Object);

            var result1 = await ClassUnderTest.GetProcessorAsync(subscription);
            var result2 = await ClassUnderTest.GetProcessorAsync(otherSubscription);

            Assert.That(result1, Is.SameAs(mockProcessor.Object));
            Assert.That(result2, Is.SameAs(otherMockProcessor.Object));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(subscription));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(otherSubscription));
        }

        [Test]
        public async Task When_getting_a_dead_letter_client_the_first_time()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClientAsync(connectionString)).ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName + "/$DeadLetterQueue", maxConcurrentCalls))
                .Returns(mockProcessor.Object);

            var result = await ClassUnderTest.GetDeadLetterProcessorAsync(subscription);

            Assert.That(result, Is.SameAs(mockProcessor.Object));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(subscription));
        }

        [Test]
        public async Task When_getting_a_dead_letter_client_after_the_first_time()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClientAsync(connectionString)).ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName + "/$DeadLetterQueue", maxConcurrentCalls))
                .Returns(mockProcessor.Object);

            var first = await ClassUnderTest.GetDeadLetterProcessorAsync(subscription);
            var result = Assert.CatchAsync(() => ClassUnderTest.GetDeadLetterProcessorAsync(subscription));

            Assert.That(first, Is.SameAs(mockProcessor.Object));
            Assert.That(result.Message, Does.Contain(subscription.ToString()));
            GetMock<ISubscriptionCreator>().Verify(x => x.CreateSubscriptionIfNecessaryAsync(IsAny<Subscription>()), Times.Once);
        }

        [Test]
        public async Task When_getting_the_single_topic_client_the_first_time()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>()
                .Setup(x => x.GetClientAsync(singleTopicConnectionString))
                .ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(singleTopicName, singleTopicSubscriptionName, maxConcurrentCalls))
                .Returns(mockProcessor.Object);

            var result = await ClassUnderTest.GetSingleTopicProcessorIfFirstTime();

            Assert.That(result, Is.SameAs(mockProcessor.Object));
        }

        [Test]
        public async Task When_getting_the_single_topic_client_after_the_first_time()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>()
                .Setup(x => x.GetClientAsync(singleTopicConnectionString))
                .ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(singleTopicName, singleTopicSubscriptionName, maxConcurrentCalls))
                .Returns(mockProcessor.Object);

            var result1 = await ClassUnderTest.GetSingleTopicProcessorIfFirstTime();
            var result2 = await ClassUnderTest.GetSingleTopicProcessorIfFirstTime();

            Assert.That(result1, Is.SameAs(mockProcessor.Object));
            Assert.That(result2, Is.Null);
        }

        [Test]
        public async Task When_disposing_and_there_are_subscriptions()
        {
            var mockProcessor = new Mock<IServiceBusProcessor>();
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClientAsync(connectionString)).ReturnsAsync(mockServiceBusClient.Object);
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(subscription.Topic.ToString(), subscription.SubscriptionName, maxConcurrentCalls))
                .Returns(mockProcessor.Object);
            var otherMockProcessor = new Mock<IServiceBusProcessor>();
            mockServiceBusClient
                .Setup(x => x.CreateProcessor(otherSubscription.Topic.ToString(), otherSubscription.SubscriptionName, maxConcurrentCalls))
                .Returns(otherMockProcessor.Object);

            await ClassUnderTest.GetProcessorAsync(subscription);
            await ClassUnderTest.GetProcessorAsync(otherSubscription);
            await ClassUnderTest.DisposeAsync();

            mockProcessor.Verify(x => x.CloseAsync());
            otherMockProcessor.Verify(x => x.CloseAsync());
        }

        [Test]
        public async Task When_disposing_and_there_are_no_subscriptions()
        {
            await ClassUnderTest.DisposeAsync();
        }
    }
}