using AutoMoqCore;
using Moq;
using NUnit.Framework;

namespace EL.ServiceBus.UnitTests
{
    public abstract class With_an_automocked<T>
    {
        private AutoMoqer mocker;

        [SetUp]
        public void WithAnAutoMockedSetup()
        {
            mocker = new AutoMoqer();
            ClassUnderTest = mocker.Create<T>();
        }

        public T ClassUnderTest { get; set; }

        protected Mock<TMock> GetMock<TMock>() where TMock : class
        {
            return mocker.GetMock<TMock>();
        }

        protected TAny Any<TAny>()
        {
            return It.IsAny<TAny>();
        }
    }
}