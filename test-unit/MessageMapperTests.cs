using System;
using System.Text;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class MessageMapperTests : With_an_automocked<MessageMapper>
    {
        [Test]
        public void When_mapping_to_service_bus_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestMessage { Data = "mapping-test" };
            var message = new Message<TestMessage>(topic, body);
            message.CorrelationId = "correlation-id";
            var serializedData = "serialized-data";
            GetMock<IMessageSerializer>().Setup(x => x.Serialize(body)).Returns(serializedData);

            var result = ClassUnderTest.ToServiceBusMessage(message);

            Assert.That(Encoding.UTF8.GetString(result.Body), Is.EqualTo(serializedData));
            Assert.That(result.MessageId, Is.EqualTo(message.MessageId));
            Assert.That(result.CorrelationId, Is.EqualTo(message.CorrelationId));
        }

        [Test]
        public void When_mapping_from_service_bus_message()
        {
            var serializedBody = "serialized-data";
            var bodyBytes = Encoding.UTF8.GetBytes(serializedBody);
            var message = new Microsoft.Azure.ServiceBus.Message(bodyBytes);
            message.MessageId = Guid.NewGuid().ToString();
            message.CorrelationId = "correlation-id";
            var topic = new Topic("el-service-bus", "test-event", 1);
            var deserialized = new TestMessage { Data = "example data" };
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<TestMessage>(serializedBody)).Returns(deserialized);

            var result = ClassUnderTest.FromServiceBusMessage<TestMessage>(topic, message);
            
            Assert.That(result.Body, Is.SameAs(deserialized));
            Assert.That(result.Topic, Is.SameAs(topic));
            Assert.That(result.MessageId, Is.EqualTo(message.MessageId));
            Assert.That(result.CorrelationId, Is.EqualTo(message.CorrelationId));
        }
    }
}