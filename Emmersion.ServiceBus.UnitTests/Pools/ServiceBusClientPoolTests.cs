using System.Threading.Tasks;
using Emmersion.ServiceBus.Pools;
using Emmersion.ServiceBus.SdkWrappers;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests.Pools
{
    internal class ServiceBusClientPoolTests : With_an_automocked<ServiceBusClientPool>
    {
        [Test]
        public async Task When_getting_a_client_for_the_first_time_a_new_one_is_created()
        {
            var connectionString = "connection-string";
            var mockClient = GetMock<IServiceBusClient>();
            GetMock<IServiceBusClientFactory>().Setup(x => x.Create(connectionString)).Returns(mockClient.Object);

            var result = await ClassUnderTest.GetClientAsync(connectionString);

            Assert.That(result, Is.SameAs(mockClient.Object));
        }
        
        [Test]
        public async Task When_getting_a_client_subsequent_times_the_same_instance_is_returned()
        {
            var connectionString = "connection-string";
            var mockClient = GetMock<IServiceBusClient>();
            GetMock<IServiceBusClientFactory>().Setup(x => x.Create(connectionString)).Returns(mockClient.Object);

            var result1 = await ClassUnderTest.GetClientAsync(connectionString);
            var result2 = await ClassUnderTest.GetClientAsync(connectionString);
            var result3 = await ClassUnderTest.GetClientAsync(connectionString);

            Assert.That(result1, Is.SameAs(mockClient.Object));
            Assert.That(result2, Is.SameAs(mockClient.Object));
            Assert.That(result3, Is.SameAs(mockClient.Object));

            GetMock<IServiceBusClientFactory>().Verify(x => x.Create(connectionString), Times.Once);
        }
        
        [Test]
        public async Task When_getting_a_client_with_different_connection_strings()
        {
            var connectionStringA = "connection-string-a";
            var connectionStringB = "connection-string-b";
            var mockClientA = GetMock<IServiceBusClient>();
            var mockClientB = GetMock<IServiceBusClient>();
            GetMock<IServiceBusClientFactory>().Setup(x => x.Create(connectionStringA)).Returns(mockClientA.Object);
            GetMock<IServiceBusClientFactory>().Setup(x => x.Create(connectionStringB)).Returns(mockClientB.Object);

            var resultA = await ClassUnderTest.GetClientAsync(connectionStringA);
            var resultB = await ClassUnderTest.GetClientAsync(connectionStringB);

            Assert.That(resultA, Is.SameAs(mockClientA.Object));
            Assert.That(resultB, Is.SameAs(mockClientB.Object));
        }
        
        [Test]
        public async Task When_disposing_and_there_are_clients()
        {
            var connectionStringA = "connection-string-a";
            var connectionStringB = "connection-string-b";
            var mockClientA = GetMock<IServiceBusClient>();
            var mockClientB = GetMock<IServiceBusClient>();
            GetMock<IServiceBusClientFactory>().Setup(x => x.Create(connectionStringA)).Returns(mockClientA.Object);
            GetMock<IServiceBusClientFactory>().Setup(x => x.Create(connectionStringB)).Returns(mockClientB.Object);
            await ClassUnderTest.GetClientAsync(connectionStringA);
            await ClassUnderTest.GetClientAsync(connectionStringB);

            await ClassUnderTest.DisposeAsync();

            mockClientA.Verify(x => x.DisposeAsync());
            mockClientB.Verify(x => x.DisposeAsync());
        }
    }
}