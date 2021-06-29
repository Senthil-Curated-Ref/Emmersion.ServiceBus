using System.Threading.Tasks;
using Emmersion.ServiceBus.Pools;
using Emmersion.ServiceBus.SdkWrappers;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests.Pools
{
    internal class ServiceBusSenderPoolTests : With_an_automocked<ServiceBusSenderPool>
    {
        [Test]
        public void When_getting_for_topic_the_first_time()
        {
            var connectionString = "connection-string";
            var topic = new Topic("example", "event", 1);
            var mockServiceBusClient = new Mock<IServiceBusClient>();
            var mockSender = new Mock<IServiceBusSender>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClient(connectionString)).Returns(mockServiceBusClient.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(topic.ToString())).Returns(mockSender.Object);

            var result = ClassUnderTest.GetForTopic(topic);

            Assert.That(result, Is.SameAs(mockSender.Object));
        }

        [Test]
        public void When_getting_for_topic_after_the_first_time()
        {
            var connectionString = "connection-string";
            var topic = new Topic("example", "event", 1);
            var mockServiceBusClient = new Mock<IServiceBusClient>();
            var mockSender = new Mock<IServiceBusSender>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClient(connectionString)).Returns(mockServiceBusClient.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(topic.ToString())).Returns(mockSender.Object);

            var result1 = ClassUnderTest.GetForTopic(topic);
            var result2 = ClassUnderTest.GetForTopic(topic);

            Assert.That(result1, Is.SameAs(mockSender.Object));
            Assert.That(result2, Is.SameAs(mockSender.Object));
            mockServiceBusClient.Verify(x => x.CreateSender(IsAny<string>()), Times.Once);
        }

        [Test]
        public void When_getting_for_topic_and_it_is_a_different_topic_name()
        {
            var connectionString = "connection-string";
            var topicA = new Topic("example", "event-a", 1);
            var topicB = new Topic("example", "event-b", 1);
            var mockServiceBusClient = new Mock<IServiceBusClient>();
            var mockSenderA = new Mock<IServiceBusSender>();
            var mockSenderB = new Mock<IServiceBusSender>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClient(connectionString)).Returns(mockServiceBusClient.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(topicA.ToString())).Returns(mockSenderA.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(topicB.ToString())).Returns(mockSenderB.Object);

            ClassUnderTest.GetForTopic(topicA);
            var result = ClassUnderTest.GetForTopic(topicB);

            Assert.That(result, Is.SameAs(mockSenderB.Object));
        }

        [Test]
        public async Task When_disposing_and_there_are_no_topics()
        {
            await ClassUnderTest.DisposeAsync();
        }

        [Test]
        public async Task When_disposing_and_there_are_topics()
        {
            var connectionString = "connection-string";
            var topicA = new Topic("example", "event-a", 1);
            var topicB = new Topic("example", "event-b", 1);
            var mockServiceBusClient = new Mock<IServiceBusClient>();
            var mockSenderA = new Mock<IServiceBusSender>();
            var mockSenderB = new Mock<IServiceBusSender>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClient(connectionString)).Returns(mockServiceBusClient.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(topicA.ToString())).Returns(mockSenderA.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(topicB.ToString())).Returns(mockSenderB.Object);
            ClassUnderTest.GetForTopic(topicA);
            ClassUnderTest.GetForTopic(topicB);

            await ClassUnderTest.DisposeAsync();

            mockSenderA.Verify(x => x.CloseAsync(), Times.Once);
            mockSenderB.Verify(x => x.CloseAsync(), Times.Once);
        }

        [Test]
        public void When_getting_for_single_topic_the_first_time()
        {
            var singleTopicConnectionString = "single-topic-connection-string";
            var singleTopicName = "single-topic-name";
            var mockServiceBusClient = new Mock<IServiceBusClient>();
            var mockSender = new Mock<IServiceBusSender>();
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicConnectionString).Returns(singleTopicConnectionString);
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicName).Returns(singleTopicName);
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClient(singleTopicConnectionString)).Returns(mockServiceBusClient.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(singleTopicName)).Returns(mockSender.Object);

            var result = ClassUnderTest.GetForSingleTopic();

            Assert.That(result, Is.SameAs(mockSender.Object));
        }

        [Test]
        public void When_getting_for_single_topic_after_the_first_time()
        {
            var singleTopicConnectionString = "single-topic-connection-string";
            var singleTopicName = "single-topic-name";
            var mockServiceBusClient = new Mock<IServiceBusClient>();
            var mockSender = new Mock<IServiceBusSender>();
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicConnectionString).Returns(singleTopicConnectionString);
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicName).Returns(singleTopicName);
            GetMock<IServiceBusClientPool>().Setup(x => x.GetClient(singleTopicConnectionString)).Returns(mockServiceBusClient.Object);
            mockServiceBusClient.Setup(x => x.CreateSender(singleTopicName)).Returns(mockSender.Object);

            var result1 = ClassUnderTest.GetForSingleTopic();
            var result2 = ClassUnderTest.GetForSingleTopic();

            Assert.That(result1, Is.SameAs(mockSender.Object));
            Assert.That(result2, Is.SameAs(mockSender.Object));
            mockServiceBusClient.Verify(x => x.CreateSender(IsAny<string>()), Times.Once);
        }
    }
}