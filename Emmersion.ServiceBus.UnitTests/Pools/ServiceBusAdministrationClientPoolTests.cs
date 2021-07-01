using System.Threading.Tasks;
using Emmersion.ServiceBus.Pools;
using Emmersion.ServiceBus.SdkWrappers;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests.Pools
{
    internal class ServiceBusAdministrationClientPoolTests : With_an_automocked<ServiceBusAdministrationClientPool>
    {
        [Test]
        public async Task When_getting_the_client_the_first_time_one_is_created()
        {
            var connectionString = "connection-string";
            var mockClient = GetMock<IServiceBusAdministrationClient>();
            GetMock<ISubscriptionConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<IServiceBusAdministrationClientFactory>().Setup(x => x.Create(connectionString)).Returns(mockClient.Object);

            var result = await ClassUnderTest.GetClientAsync();
            
            Assert.That(result, Is.SameAs(mockClient.Object));
        }
        
        [Test]
        public async Task When_getting_the_client_subsequent_times_it_is_only_created_once()
        {
            var connectionString = "connection-string";
            var mockClient = GetMock<IServiceBusAdministrationClient>();
            GetMock<ISubscriptionConfig>().Setup(x => x.ConnectionString).Returns(connectionString);
            GetMock<IServiceBusAdministrationClientFactory>().Setup(x => x.Create(connectionString)).Returns(mockClient.Object);

            var result1 = await ClassUnderTest.GetClientAsync();
            var result2 = await ClassUnderTest.GetClientAsync();
            var result3 = await ClassUnderTest.GetClientAsync();
            
            Assert.That(result1, Is.SameAs(mockClient.Object));
            Assert.That(result2, Is.SameAs(mockClient.Object));
            Assert.That(result3, Is.SameAs(mockClient.Object));
            GetMock<IServiceBusAdministrationClientFactory>().Verify(x => x.Create(IsAny<string>()), Times.Once);
        }
    }
}