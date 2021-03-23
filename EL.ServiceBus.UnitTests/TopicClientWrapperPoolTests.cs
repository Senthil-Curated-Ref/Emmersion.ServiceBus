using EL.Testing;
using Moq;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class TopicClientWrapperPoolTests : With_an_automocked<TopicClientWrapperPool>
    {
        [Test]
        public void When_getting_for_topic_the_first_time()
        {
            var connectionString = "connection-string";
            var topic = new Topic("example", "event", 1);
            var mockWrapper = new Mock<ITopicClientWrapper>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topic.ToString())).Returns(mockWrapper.Object);

            var result = ClassUnderTest.GetForTopic(topic);

            Assert.That(result, Is.SameAs(mockWrapper.Object));
        }

        [Test]
        public void When_getting_for_topic_after_the_first_time()
        {
            var connectionString = "connection-string";
            var topic = new Topic("example", "event", 1);
            var mockWrapper = new Mock<ITopicClientWrapper>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topic.ToString())).Returns(mockWrapper.Object);

            var result1 = ClassUnderTest.GetForTopic(topic);
            var result2 = ClassUnderTest.GetForTopic(topic);

            Assert.That(result1, Is.SameAs(mockWrapper.Object));
            Assert.That(result2, Is.SameAs(mockWrapper.Object));
            GetMock<ITopicClientWrapperCreator>().Verify(x => x.Create(IsAny<string>(), IsAny<string>()), Times.Once);
        }

        [Test]
        public void When_getting_for_topic_and_it_is_a_different_topic_name()
        {
            var connectionString = "connection-string";
            var topicA = new Topic("example", "event-a", 1);
            var topicB = new Topic("example", "event-b", 1);
            var mockWrapperA = new Mock<ITopicClientWrapper>();
            var mockWrapperB = new Mock<ITopicClientWrapper>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicA.ToString())).Returns(mockWrapperA.Object);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicB.ToString())).Returns(mockWrapperB.Object);
            
            ClassUnderTest.GetForTopic(topicA);
            var result = ClassUnderTest.GetForTopic(topicB);

            Assert.That(result, Is.SameAs(mockWrapperB.Object));
        }

        [Test]
        public void When_disposing_and_there_are_no_topics()
        {
            Assert.DoesNotThrow(() => ClassUnderTest.Dispose());
        }

        [Test]
        public void When_disposing_and_there_are_topics()
        {
            var connectionString = "connection-string";
            var topicA = new Topic("example", "event-a", 1);
            var topicB = new Topic("example", "event-b", 1);
            var mockWrapperA = new Mock<ITopicClientWrapper>();
            var mockWrapperB = new Mock<ITopicClientWrapper>();
            GetMock<IPublisherConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicA.ToString())).Returns(mockWrapperA.Object);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicB.ToString())).Returns(mockWrapperB.Object);
            ClassUnderTest.GetForTopic(topicA);
            ClassUnderTest.GetForTopic(topicB);

            ClassUnderTest.Dispose();

            mockWrapperA.Verify(x => x.CloseAsync(), Times.Once);
            mockWrapperB.Verify(x => x.CloseAsync(), Times.Once);
        }

        [Test]
        public void When_getting_for_single_topic_the_first_time()
        {
            var singleTopicConnectionString = "single-topic-connection-string";
            var singleTopicName = "single-topic-name";
            var mockWrapper = new Mock<ITopicClientWrapper>();
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicConnectionString).Returns(singleTopicConnectionString);
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicName).Returns(singleTopicName);
            GetMock<ITopicClientWrapperCreator>()
                .Setup(x => x.Create(singleTopicConnectionString, singleTopicName))
                .Returns(mockWrapper.Object);

            var result = ClassUnderTest.GetForSingleTopic();

            Assert.That(result, Is.SameAs(mockWrapper.Object));
        }

        [Test]
        public void When_getting_for_single_topic_after_the_first_time()
        {
            var singleTopicConnectionString = "single-topic-connection-string";
            var singleTopicName = "single-topic-name";
            var mockWrapper = new Mock<ITopicClientWrapper>();
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicConnectionString).Returns(singleTopicConnectionString);
            GetMock<IPublisherConfig>().Setup(x => x.SingleTopicName).Returns(singleTopicName);
            GetMock<ITopicClientWrapperCreator>()
                .Setup(x => x.Create(singleTopicConnectionString, singleTopicName))
                .Returns(mockWrapper.Object);

            var result1 = ClassUnderTest.GetForSingleTopic();
            var result2 = ClassUnderTest.GetForSingleTopic();

            Assert.That(result1, Is.SameAs(mockWrapper.Object));
            Assert.That(result2, Is.SameAs(mockWrapper.Object));
            GetMock<ITopicClientWrapperCreator>().Verify(x => x.Create(IsAny<string>(), IsAny<string>()), Times.Once);
        }
    }
}