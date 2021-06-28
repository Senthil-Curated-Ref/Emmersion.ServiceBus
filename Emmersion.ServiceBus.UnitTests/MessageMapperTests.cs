using System;
using System.Text;
using Emmersion.Testing;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests
{
    internal class MessageMapperTests : With_an_automocked<MessageMapper>
    {
        [Test]
        public void When_mapping_to_service_bus_message()
        {
            var topic = new Topic("el-service-bus", "test-event", 1);
            var body = new TestData { Data = "mapping-test" };
            var message = new Message<TestData>(topic, body)
            {
                PublishedAt = DateTimeOffset.UtcNow,
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(5),
                Environment = "unit-tests"
            };
            message.CorrelationId = "correlation-id";
            var serializedData = "serialized-data";
            Payload<TestData> payload = null;
            GetMock<IMessageSerializer>()
                .Setup(x => x.Serialize(IsAny<Payload<TestData>>()))
                .Callback((Payload<TestData> x) => payload = x)
                .Returns(serializedData);

            var result = ClassUnderTest.ToServiceBusMessage(message);

            Assert.That(payload.Body, Is.SameAs(message.Body));
            Assert.That(payload.PublishedAt, Is.EqualTo(message.PublishedAt));
            Assert.That(payload.EnqueuedAt, Is.EqualTo(message.EnqueuedAt));
            Assert.That(payload.Environment, Is.EqualTo(message.Environment));
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
            message.MessageId = RandomString();
            message.CorrelationId = "correlation-id";
            var topic = new Topic("el-service-bus", "test-event", 1);
            var deserialized = new Payload<TestData> {
                Body = new TestData { Data = "example data" },
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                Environment = "unit-tests"
            };
            var receivedAt = DateTimeOffset.UtcNow;
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<Payload<TestData>>(serializedBody)).Returns(deserialized);

            var result = ClassUnderTest.FromServiceBusMessage<TestData>(topic, message, receivedAt);
            
            Assert.That(result.Body, Is.SameAs(deserialized.Body));
            Assert.That(result.Topic, Is.SameAs(topic));
            Assert.That(result.MessageId, Is.EqualTo(message.MessageId));
            Assert.That(result.CorrelationId, Is.EqualTo(message.CorrelationId));
            Assert.That(result.PublishedAt, Is.EqualTo(deserialized.PublishedAt), "Incorrect PublishedAt");
            Assert.That(result.EnqueuedAt, Is.EqualTo(deserialized.EnqueuedAt), "Incorrect EnqueuedAt");
            Assert.That(result.ReceivedAt, Is.EqualTo(receivedAt), "Incorrect ReceivedAt");
            Assert.That(result.Environment, Is.EqualTo(deserialized.Environment));
        }

        [Test]
        public void When_mapping_a_dead_letter()
        {
            var serializedBody = "serialized-data";
            var bodyBytes = Encoding.UTF8.GetBytes(serializedBody);
            var message = new Microsoft.Azure.ServiceBus.Message(bodyBytes)
            {
                MessageId = RandomString(),
                CorrelationId = RandomString()
            };

            var result = ClassUnderTest.GetDeadLetter(message);

            Assert.That(result.MessageId, Is.EqualTo(message.MessageId));
            Assert.That(result.CorrelationId, Is.EqualTo(message.CorrelationId));
            Assert.That(result.Body, Is.EqualTo(serializedBody));
        }

        [Test]
        public void When_mapping_from_message_envelope()
        {
            var envelope = new MessageEnvelope<TestData>();
            var serialized = "serialized-data";
            GetMock<IMessageSerializer>().Setup(x => x.Serialize(envelope)).Returns(serialized);

            var result = ClassUnderTest.FromMessageEnvelope(envelope);

            Assert.That(Encoding.UTF8.GetString(result.Body), Is.EqualTo(serialized));
        }

        [Test]
        public void When_mapping_to_message_envelope()
        {
            var body = "test-message-body";
            var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(body));
            var envelope = new MessageEnvelope<TestData>();
            GetMock<IMessageSerializer>().Setup(x => x.Deserialize<MessageEnvelope<TestData>>(body)).Returns(envelope);

            var result = ClassUnderTest.ToMessageEnvelope<TestData>(message);

            Assert.That(result, Is.SameAs(envelope));
        }
    }
}