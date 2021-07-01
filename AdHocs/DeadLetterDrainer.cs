using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace AdHocs
{
    class DeadLetterDrainer
    {
        static int Count = 0;
        static ConcurrentBag<string> MessageBuffer = new ConcurrentBag<string>();
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);
        static string Filename;

        public async Task Drain()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<DeadLetterDrainer>()
                .Build();
            var connectionString = config.GetValue<string>("ServiceBus:ConnectionString");
            var topic = config.GetValue<string>("ServiceBus:TopicName");
            var subscription = config.GetValue<string>("ServiceBus:SubscriptionName");
            var deadLetterSubscription = subscription + "/$DeadLetterQueue";
            Filename = $"{topic}-{subscription}-dead-letters.txt";

            Console.WriteLine($"Draining dead letters from {topic}/{deadLetterSubscription}");
            
            await using var client = new ServiceBusClient(connectionString);
            var processor = client.CreateProcessor(topic, deadLetterSubscription, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 5
            });
            processor.ProcessMessageAsync += AddMessageToBuffer;
            processor.ProcessErrorAsync += WriteException;
            await processor.StartProcessingAsync();

            await using var timer = new Timer(WriteBuffer, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            Console.WriteLine("Press ENTER to stop.");
            await Console.In.ReadLineAsync();
            
            await processor.CloseAsync();
            WriteBuffer(null);
            Console.WriteLine("Stopped.");
        }

        private static async void WriteBuffer(object state)
        {
            if (MessageBuffer.Count > 0)
            {
                await semaphoreSlim.WaitAsync();
                if (MessageBuffer.Count > 0)
                {
                    await File.AppendAllLinesAsync(Filename, MessageBuffer.ToArray());
                    MessageBuffer.Clear();
                }
            }
        }

        private static Task WriteException(ProcessErrorEventArgs args)
        {
            Console.WriteLine("EXCEPTION:");
            Console.WriteLine(args.Exception.Message);
            return Task.CompletedTask;
        }

        private static Task AddMessageToBuffer(ProcessMessageEventArgs args)
        {
            MessageBuffer.Add(Encoding.UTF8.GetString(args.Message.Body) + Environment.NewLine);

            Count++;
            var count = Count;
            if (count % 100 == 0) {
                Console.Write(".");
            }
            if (count % 1000 == 0) {
                Console.WriteLine($" {Count} messages received ({DateTimeOffset.Now:O})");
            }
            return Task.CompletedTask;
        }
    }
}
