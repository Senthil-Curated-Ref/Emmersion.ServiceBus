using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class SubscriptionTests
    {
        [TestCase("assessments", "sign-in-handler")]
        [TestCase("data-context", "monlith-listener")]
        public void When_creating_a_subscription(string productContext, string process)
        {
            var topic = new Topic("monolith", "user-signed-in", 1);
            var subscription = new Subscription(topic, productContext, process);

            Assert.That(subscription.Topic, Is.SameAs(topic));
            Assert.That(subscription.SubscriptionName, Is.EqualTo($"{productContext}.{process}"));
            Assert.That(subscription.ToString(), Is.EqualTo($"{topic}=>{subscription.SubscriptionName}"));
        }

        [Test]
        public void When_attempting_to_create_a_subscription_with_an_invalid_topic()
        {
            var exception = Assert.Catch(() => new Subscription(null, "test", "listener"));

            Assert.That(exception.Message, Is.EqualTo("Topic may not be null (Parameter 'topic')"));
        }

        [TestCase("Assessments", "sign-in-handler")]
        [TestCase(null, "sign-in-handler")]
        [TestCase("data.context", "monlith-listener")]
        [TestCase("data context", "monlith-listener")]
        public void When_attempting_to_create_a_subscription_with_an_invalid_product_context(string productContext, string process)
        {
            var topic = new Topic("monolith", "user-signed-in", 1);

            var exception = Assert.Catch(() => new Subscription(topic, productContext, process));

            Assert.That(exception.Message, Is.EqualTo("Product Context name must match pattern: " + Topic.Pattern + " (Parameter 'productContext')"));
        }

        [TestCase("assessments", "")]
        [TestCase("data-context", "^MONOLITH")]
        [TestCase("insights", null)]
        public void When_attempting_to_create_a_subscription_with_an_invalid_process_name(string productContext, string process)
        {
            var topic = new Topic("monolith", "user-signed-in", 1);

            var exception = Assert.Catch(() => new Subscription(topic, productContext, process));

            Assert.That(exception.Message, Is.EqualTo("Process name must match pattern: " + Topic.Pattern + " (Parameter 'process')"));
        }

        [Test]
        public void When_getting_a_dead_letter_queue()
        {
            var topic = new Topic("monolith", "user-signed-in", 1);
            var subscription = new Subscription(topic, "other-context", "listener");
            
            var deadLetterSubscription = subscription.GetDeadLetterQueue();

            Assert.That(deadLetterSubscription.Topic, Is.EqualTo(topic));
            Assert.That(deadLetterSubscription.SubscriptionName, Is.EqualTo($"{subscription.SubscriptionName}/$DeadLetterQueue"));
            Assert.That(deadLetterSubscription.ToString(), Is.EqualTo($"{subscription}/$DeadLetterQueue"));
        }
    }
}