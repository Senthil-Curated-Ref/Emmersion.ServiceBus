using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace AdHocs
{
    class DeadLetterDrainer
    {
        static int Count = 0;
        static ConcurrentBag<string> MessageBuffer = new ConcurrentBag<string>();
        static object LockObject = new object();
        static string Filename;

        public void Drain()
        {
            var overridesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "truenorth-overrides/El.ServiceBus", "AdHoc-appsettings.json");
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile(overridesPath, optional: true)
                .Build();
            var connectionString = config.GetValue<string>("ConnectionString");
            var topic = config.GetValue<string>("TopicName");
            var subscription = config.GetValue<string>("SubscriptionName");
            var deadLetterSubscription = subscription + "/$DeadLetterQueue";
            Filename = $"{topic}-{subscription}-dead-letters.txt";

            Console.WriteLine($"Draining dead letters from {topic}/{deadLetterSubscription}");
            
            var client = new SubscriptionClient(connectionString, topic, deadLetterSubscription);
            var options = new MessageHandlerOptions(WriteException)
            {
                MaxConcurrentCalls = 5
            };
            client.RegisterMessageHandler((message, _) => AddMessageToBuffer(message), options);

            var timer = new Timer(WriteBuffer, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            Console.WriteLine("Press ENTER to stop.");
            Console.ReadLine();

            client.CloseAsync().Wait();
            timer.Dispose();
            WriteBuffer(null);
            Console.WriteLine("Stopped.");
        }

        private static void WriteBuffer(object state)
        {
            if (MessageBuffer.Count > 0)
            {
                lock(LockObject)
                {
                    if (MessageBuffer.Count > 0)
                    {
                        File.AppendAllLinesAsync(Filename, MessageBuffer.ToArray()).Wait();
                        MessageBuffer.Clear();
                    }
                }
                
            }
        }

        private static Task WriteException(ExceptionReceivedEventArgs args)
        {
            Console.WriteLine("EXCEPTION:");
            Console.WriteLine(args.Exception.Message);
            return Task.CompletedTask;
        }

        private static Task AddMessageToBuffer(Message message)
        {
            MessageBuffer.Add(Encoding.UTF8.GetString(message.Body) + Environment.NewLine);

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
