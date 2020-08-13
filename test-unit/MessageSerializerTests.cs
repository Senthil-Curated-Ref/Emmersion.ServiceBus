using System;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    internal class MessageSerializerTests : With_an_automocked<MessageSerializer>
    {
        [Test]
        public void When_serializing_and_deserializing()
        {
            var original = new SerializationTest<SerializationTest<int>>
            {
                IntData = 1,
                StringData = "hello",
                TemporalData = DateTimeOffset.UtcNow,
                GenericData = new SerializationTest<int>
                {
                    IntData = 2,
                    StringData = "world",
                    TemporalData = DateTimeOffset.UtcNow.AddMinutes(5),
                    GenericData = -1
                }
            };

            var serialized = ClassUnderTest.Serialize(original);
            var deserialized = ClassUnderTest.Deserialize<SerializationTest<SerializationTest<int>>>(serialized);

            Assert.That(deserialized.IntData, Is.EqualTo(original.IntData));
            Assert.That(deserialized.StringData, Is.EqualTo(original.StringData));
            Assert.That(deserialized.TemporalData, Is.EqualTo(original.TemporalData));
            Assert.That(deserialized.GenericData.IntData, Is.EqualTo(original.GenericData.IntData));
            Assert.That(deserialized.GenericData.StringData, Is.EqualTo(original.GenericData.StringData));
            Assert.That(deserialized.GenericData.TemporalData, Is.EqualTo(original.GenericData.TemporalData));
            Assert.That(deserialized.GenericData.GenericData, Is.EqualTo(original.GenericData.GenericData));
        }
    }

    internal class SerializationTest<T>
    {
        public int IntData { get; set; }
        public string StringData { get; set; }
        public DateTimeOffset TemporalData { get; set; }
        public T GenericData { get; set; }
    }
}