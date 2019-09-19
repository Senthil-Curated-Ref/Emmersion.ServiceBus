using El.ServiceBus;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    public class MessageEventTests
    {
        [TestCase("test-event", 1)]
        [TestCase("test.event", 2)]
        [TestCase("great.test.event", 700)]
        public void When_creating_a_valid_event(string eventName, int version)
        {
            var messageEvent = new MessageEvent(eventName, version);

            Assert.That(messageEvent.ToString(), Is.EqualTo($"{eventName}.v{version}"));
        }

        [TestCase("TestEvent", 1)]
        [TestCase("such!event", 2)]
        [TestCase(".bad", 1)]
        [TestCase("-bad", 1)]
        [TestCase("still-bad-", 1)]
        [TestCase("still.bad-", 5)]
        [TestCase("invalid-version", -1)]
        public void When_attempting_to_create_an_event_with_invalid_name_or_version(string eventName, int version)
        {
            var exception = Assert.Catch(() => new MessageEvent(eventName, version));
        }
    }
}