using System;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    public delegate void OnServiceBusException(object source, ServiceBusExceptionArgs args);

    public class ServiceBusExceptionArgs
    {
        public ServiceBusExceptionArgs(Subscription subscription, Exception exception, string action, string clientId, string endpoint, string entityPath)
        {
            Subscription = subscription;
            Exception = exception;
            Action = action;
            ClientId = clientId;
            Endpoint = endpoint;
            EntityPath = entityPath;
        }

        internal ServiceBusExceptionArgs(Subscription subscription, ExceptionReceivedEventArgs args)
        {
            Subscription = subscription;
            Exception = args.Exception;
            Action = args.ExceptionReceivedContext?.Action ?? "";
            ClientId = args.ExceptionReceivedContext?.ClientId ?? "";
            Endpoint = args.ExceptionReceivedContext?.Endpoint ?? "";
            EntityPath = args.ExceptionReceivedContext?.EntityPath ?? "";
        }

        public Subscription Subscription { get; }
        public Exception Exception { get; }
        public string Action { get; }
        public string ClientId { get; }
        public string Endpoint { get; }
        public string EntityPath { get; }
    }
}
