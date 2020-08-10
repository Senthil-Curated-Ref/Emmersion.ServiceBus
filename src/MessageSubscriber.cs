using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    public interface IMessageSubscriber
    {
        void Subscribe<T>(MessageEvent messageEvent, Action<T> action);
        event OnMessageReceived OnMessageReceived;
        event OnUnhandledException OnUnhandledException;
        event OnServiceBusException OnServiceBusException;
    }

    internal class MessageSubscriber : IMessageSubscriber
    {
        private readonly List<RoutingSubscription> subscriptions = new List<RoutingSubscription>();
        private readonly ISubscriptionClientWrapper subscriptionClientWrapper;
        private readonly IMessageSerializer messageSerializer;
        public event OnMessageReceived OnMessageReceived;
        public event OnUnhandledException OnUnhandledException;
        public event OnServiceBusException OnServiceBusException;

        public MessageSubscriber(ISubscriptionClientWrapper subscriptionClientWrapper, IMessageSerializer messageSerializer)
        {
            this.subscriptionClientWrapper = subscriptionClientWrapper;
            this.messageSerializer = messageSerializer;

            subscriptionClientWrapper.Subscribe(RouteMessage, HandleException);
        }

        public void Subscribe<T>(MessageEvent messageEvent, Action<T> action)
        {
            subscriptions.Add(new RoutingSubscription
            {
                MessageEvent = messageEvent.ToString(),
                Action = (serializedMessage) =>
                {
                    var envelope = messageSerializer.Deserialize<MessageEnvelope<T>>(serializedMessage);
                    action(envelope.Payload);
                }
            });
        }

        internal void RouteMessage(string serializedMessage)
        {
            var receivedAt = DateTimeOffset.UtcNow;
            var envelope = messageSerializer.Deserialize<MessageEnvelope<object>>(serializedMessage);
            var recipients = subscriptions.Where(x => x.MessageEvent == envelope.MessageEvent).ToList();
            try
            {
                recipients.ForEach(x => x.Action(serializedMessage));
            }
            catch (Exception e)
            {
                OnUnhandledException?.Invoke(this, new UnhandledExceptionArgs(envelope.MessageEvent, e));
                throw;
            }
            finally
            {
                var processingTime = DateTimeOffset.UtcNow - receivedAt;
                OnMessageReceived?.Invoke(this, new MessageReceivedArgs(
                    envelope.MessageEvent,
                    envelope.PublishedAt,
                    receivedAt,
                    processingTime,
                    recipients.Count
                ));
            }
        }

        internal void HandleException(ExceptionReceivedEventArgs args)
        {
            OnServiceBusException?.Invoke(this, new ServiceBusExceptionArgs(args));
        }
    }

    internal class RoutingSubscription
    {
        public string MessageEvent { get; set; }
        public Action<string> Action { get; set; }
    }
}
