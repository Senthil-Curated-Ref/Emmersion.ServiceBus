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
            var topicName = "topic-name";
            var mockWrapper = new Mock<ITopicClientWrapper>();
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicName)).Returns(mockWrapper.Object);

            var result = ClassUnderTest.GetForTopic(connectionString, topicName);

            Assert.That(result, Is.SameAs(mockWrapper.Object));
        }

        [Test]
        public void When_getting_for_topic_after_the_first_time()
        {
            var connectionString = "connection-string";
            var topicName = "topic-name";
            var mockWrapper = new Mock<ITopicClientWrapper>();
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicName)).Returns(mockWrapper.Object);

            var result1 = ClassUnderTest.GetForTopic(connectionString, topicName);
            var result2 = ClassUnderTest.GetForTopic(connectionString, topicName);

            Assert.That(result1, Is.SameAs(mockWrapper.Object));
            Assert.That(result2, Is.SameAs(mockWrapper.Object));
            GetMock<ITopicClientWrapperCreator>().Verify(x => x.Create(Any<string>(), Any<string>()), Times.Once);
        }

        [Test]
        public void When_getting_for_topic_and_it_is_a_different_topic_name()
        {
            var connectionString = "connection-string";
            var topicNameA = "topic-a";
            var topicNameB = "topic-b";
            var mockWrapperA = new Mock<ITopicClientWrapper>();
            var mockWrapperB = new Mock<ITopicClientWrapper>();
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicNameA)).Returns(mockWrapperA.Object);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicNameB)).Returns(mockWrapperB.Object);
            
            ClassUnderTest.GetForTopic(connectionString, topicNameA);
            var result = ClassUnderTest.GetForTopic(connectionString, topicNameB);

            Assert.That(result, Is.SameAs(mockWrapperB.Object));
        }

        [Test]
        public void When_disposing_an_there_are_no_topics()
        {
            Assert.DoesNotThrow(() => ClassUnderTest.Dispose());
        }

        [Test]
        public void When_disposing_an_there_are_topics()
        {
            var connectionString = "connection-string";
            var topicNameA = "topic-a";
            var topicNameB = "topic-b";
            var mockWrapperA = new Mock<ITopicClientWrapper>();
            var mockWrapperB = new Mock<ITopicClientWrapper>();
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicNameA)).Returns(mockWrapperA.Object);
            GetMock<ITopicClientWrapperCreator>().Setup(x => x.Create(connectionString, topicNameB)).Returns(mockWrapperB.Object);
            ClassUnderTest.GetForTopic(connectionString, topicNameA);
            ClassUnderTest.GetForTopic(connectionString, topicNameB);

            ClassUnderTest.Dispose();

            mockWrapperA.Verify(x => x.CloseAsync(), Times.Once);
            mockWrapperB.Verify(x => x.CloseAsync(), Times.Once);
        }
    }
}