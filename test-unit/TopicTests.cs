using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    public class TopicTests
    {
        [TestCase("monolith", "test-event", 1)]
        [TestCase("assessments", "user-assessment-started", 2)]
        [TestCase("zero-wing", "for-great-justice", 700)]
        public void When_creating_a_valid_topic(string productContext, string eventName, int version)
        {
            var topic = new Topic(productContext, eventName, version);

            Assert.That(topic.ToString(), Is.EqualTo($"{productContext}.{eventName}.v{version}"));
        }

        [TestCase("Monolith", "test-event", 1)]
        [TestCase("1assessments", "user-assessment-started", 2)]
        [TestCase("zero.wing", "for-great-justice", 700)]
        public void When_attempting_to_create_a_topic_with_invalid_product_context(string productContext, string eventName, int version)
        {
            var exception = Assert.Catch(() => new Topic(productContext, eventName, version));

            Assert.That(exception.Message, Is.EqualTo("Product Context name must match pattern: " + Topic.Pattern + " (Parameter 'productContext')"));
        }

        [TestCase("monolith", "test-Event", 1)]
        [TestCase("assessments", "user-assessment-started1", 2)]
        [TestCase("zero-wing", "for-great@justice", 700)]
        public void When_attempting_to_create_a_topic_with_invalid_event_name(string productContext, string eventName, int version)
        {
            var exception = Assert.Catch(() => new Topic(productContext, eventName, version));

            Assert.That(exception.Message, Is.EqualTo("Event name must match pattern: " + Topic.Pattern + " (Parameter 'eventName')"));
        }

        [TestCase("monolith", "test-event", -1)]
        [TestCase("assessments", "user-assessment-started", -2)]
        public void When_attempting_to_create_a_topic_with_invalid_version(string productContext, string eventName, int version)
        {
            var exception = Assert.Catch(() => new Topic(productContext, eventName, version));

            Assert.That(exception.Message, Is.EqualTo("Version may not be negative (Parameter 'version')"));
        }

        [Test]
        public void When_attempting_to_create_a_topic_with_a_name_which_is_too_long_for_azure()
        {
            var exception = Assert.Catch(() => new Topic(
                "an-extremely-long-name-for-a-very-particular-product-context-which-makes-us-sad-because-keeping-track-of-it-is-such-a-mouthful-all-the-time", 
                "an-even-longer-name-for-a-very-specific-event-which-probably-only-happens-at-scale-and-might-never-occur-once",
                1234567890));

            Assert.That(exception.Message, Is.EqualTo("The full name of the topic may not exceed the Azure 260 character limitation"));
        }
    }
}