using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus.SdkWrappers
{
    internal interface IServiceBusClient : IAsyncDisposable
    {
        ServiceBusSender CreateSender(string topicName);
        ServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, int maxConcurrentCalls);
    }

    internal class ServiceBusClientWrapper : IServiceBusClient
    {
        private readonly ServiceBusClient serviceBusClient;

        public ServiceBusClientWrapper(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"Invalid connectionString", nameof(connectionString));
            }
            
            serviceBusClient = new ServiceBusClient(connectionString);
        }

        public ServiceBusSender CreateSender(string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
            {
                throw new ArgumentException($"Invalid topicName", nameof(topicName));
            }
            
            return serviceBusClient.CreateSender(topicName);
        }

        public ServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, int maxConcurrentMessages)
        {
            if (string.IsNullOrEmpty(topicName))
            {
                throw new ArgumentException($"Invalid topic", nameof(topicName));
            }
            if (string.IsNullOrEmpty(subscriptionName))
            {
                throw new ArgumentException($"Invalid subscription", nameof(subscriptionName));
            }
            if (maxConcurrentMessages < 1)
            {
                throw new ArgumentException($"MaxConcurrentMessages must be greater than zero.", nameof(maxConcurrentMessages));
            }
            
            return serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = maxConcurrentMessages
            });
        }

        public async ValueTask DisposeAsync()
        {
            await serviceBusClient.DisposeAsync();
        }
    }
}