using System.Collections.Generic;
using System.Threading.Tasks;
using Emmersion.ServiceBus.Pools;
using NUnit.Framework;

namespace Emmersion.ServiceBus.UnitTests.Pools
{
    public class SemaphoreLockedPoolTests
    {
        private SemaphoreLockedPool<int> classUnderTest;

        [SetUp]
        public void SetUp()
        {
            classUnderTest = new SemaphoreLockedPool<int>();
        }
        
        [Test]
        public async Task When_getting_an_item_and_it_does_not_exist()
        {
            var result = await classUnderTest.Get("testing", () => Task.FromResult(1));

            Assert.That(result.Item, Is.EqualTo(1));
            Assert.That(result.NewlyCreated, Is.True);
        }

        [Test]
        public async Task When_getting_an_item_after_it_exists()
        {
            var result1 = await classUnderTest.Get("test-key", () => Task.FromResult(1));
            var result2 = await classUnderTest.Get("test-key", () => Task.FromResult(2));
            var result3 = await classUnderTest.Get("test-key", () => Task.FromResult(3));
            
            Assert.That(result1.Item, Is.EqualTo(1));
            Assert.That(result1.NewlyCreated, Is.True);
            Assert.That(result2.Item, Is.EqualTo(1));
            Assert.That(result2.NewlyCreated, Is.False);
            Assert.That(result3.Item, Is.EqualTo(1));
            Assert.That(result3.NewlyCreated, Is.False);
        }

        [Test]
        public async Task When_getting_items_with_different_keys()
        {
            var result1 = await classUnderTest.Get("key-1", () => Task.FromResult(1));
            var result2 = await classUnderTest.Get("key-2", () => Task.FromResult(2));
            var result3 = await classUnderTest.Get("key-3", () => Task.FromResult(3));
            
            Assert.That(result1.Item, Is.EqualTo(1));
            Assert.That(result1.NewlyCreated, Is.True);
            Assert.That(result2.Item, Is.EqualTo(2));
            Assert.That(result2.NewlyCreated, Is.True);
            Assert.That(result3.Item, Is.EqualTo(3));
            Assert.That(result3.NewlyCreated, Is.True);
        }

        [Test]
        public async Task When_clearing_a_lambda_is_called_for_each_item()
        {
            await classUnderTest.Get("key-1", () => Task.FromResult(1));
            await classUnderTest.Get("key-2", () => Task.FromResult(2));
            await classUnderTest.Get("key-3", () => Task.FromResult(3));
            await classUnderTest.Get("key-1", () => Task.FromResult(11));
            var disposed = new List<int>();

            await classUnderTest.Clear(item =>
            {
                disposed.Add(item);
                return Task.CompletedTask;
            });

            Assert.That(disposed, Is.EquivalentTo(new[] {1, 2, 3}));
        }

        [Test]
        public async Task When_clearing_the_pool_is_emptied()
        {
            var result1 = await classUnderTest.Get("test-key", () => Task.FromResult(1));
            var result2 = await classUnderTest.Get("test-key", () => Task.FromResult(2));

            await classUnderTest.Clear(item => Task.CompletedTask);
            
            var result3 = await classUnderTest.Get("test-key", () => Task.FromResult(3));

            Assert.That(result1.Item, Is.EqualTo(1));
            Assert.That(result2.Item, Is.EqualTo(1));
            Assert.That(result3.Item, Is.EqualTo(3));
        }
    }
}