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
            var message = new Message<TestMessage>(topic, body)
            {
                PublishedAt = DateTimeOffset.UtcNow,
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            message.CorrelationId = "correlation-id";
            var serializedData = "serialized-data";
            Payload<TestMessage> payload = null;
            GetMock<IMessageSerializer>()
                .Setup(x => x.Serialize(Any<Payload<TestMessage>>()))
                .Callback((Payload<TestMessage> x) => payload = x)
                .Returns(serializedData);

            var result = ClassUnderTest.ToServiceBusMessage(message);

            Assert.That(payload.Body, Is.SameAs(message.Body));
            Assert.That(payload.PublishedAt, Is.EqualTo(message.PublishedAt));
            Assert.That(payload.EnqueuedAt, Is.EqualTo(message.EnqueuedAt));
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
            var deserialized = new Payload<TestMessage> {
                Body = new TestMessage { Data = "example data" },
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-5)   
            };
            var receivedAt = DateTimeOffset.UtcNow;
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<Payload<TestMessage>>(serializedBody)).Returns(deserialized);

            var result = ClassUnderTest.FromServiceBusMessage<TestMessage>(topic, message, receivedAt);
            
            Assert.That(result.Body, Is.SameAs(deserialized.Body));
            Assert.That(result.Topic, Is.SameAs(topic));
            Assert.That(result.MessageId, Is.EqualTo(message.MessageId));
            Assert.That(result.CorrelationId, Is.EqualTo(message.CorrelationId));
            Assert.That(result.PublishedAt, Is.EqualTo(deserialized.PublishedAt), "Incorrect PublishedAt");
            Assert.That(result.EnqueuedAt, Is.EqualTo(deserialized.EnqueuedAt), "Incorrect EnqueuedAt");
            Assert.That(result.ReceivedAt, Is.EqualTo(receivedAt), "Incorrect ReceivedAt");
        }

        [Test]
        public void When_mapping_to_message_envelope()
        {
            var body = "test-message-body";
            var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(body));
            var envelope = new MessageEnvelope<TestMessage>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<TestMessage>>(body)).Returns(envelope);

            var result = ClassUnderTest.ToMessageEnvelope<TestMessage>(message);

            Assert.That(result, Is.SameAs(envelope));
        }
    }
}