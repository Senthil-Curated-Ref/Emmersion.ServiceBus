using System;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus
{
    public delegate void OnException(object source, ExceptionArgs args);

    public class ExceptionArgs
    {
        public ExceptionArgs(Subscription subscription, Exception exception, string action, string clientId, string endpoint, string entityPath)
        {
            Subscription = subscription;
            Exception = exception;
            Action = action;
            ClientId = clientId;
            Endpoint = endpoint;
            EntityPath = entityPath;
        }

        internal ExceptionArgs(Subscription subscription, ProcessErrorEventArgs args)
        {
            Subscription = subscription;
            Exception = args.Exception;
            Action = "";
            ClientId = "";
            Endpoint = "";
            EntityPath = args.EntityPath ?? "";
        }

        public Subscription Subscription { get; }
        public Exception Exception { get; }
        public string Action { get; }
        public string ClientId { get; }
        public string Endpoint { get; }
        public string EntityPath { get; }
    }
}
