using System;
using Azure.Messaging.ServiceBus;

namespace Emmersion.ServiceBus
{
    public delegate void OnException(object source, ExceptionArgs args);

    public class ExceptionArgs
    {
        [Obsolete("This constructor takes data which is no longer provided by the Azure SDK")]
        public ExceptionArgs(Subscription subscription, Exception exception, string action, string clientId, string endpoint, string entityPath)
        {
            Subscription = subscription;
            Exception = exception;
            Action = action;
            ClientId = clientId;
            Endpoint = endpoint;
            EntityPath = entityPath;
        }
        
        public ExceptionArgs(Subscription subscription, Exception exception, string entityPath, string errorSource, string fullyQualifiedNamespace)
        {
            Subscription = subscription;
            Exception = exception;
            EntityPath = entityPath;
            ErrorSource = errorSource;
            FullyQualifiedNamespace = fullyQualifiedNamespace;
        }

        internal ExceptionArgs(Subscription subscription, ProcessErrorEventArgs args)
        {
            Subscription = subscription;
            Exception = args.Exception;
            EntityPath = args.EntityPath ?? "";
            ErrorSource = args.ErrorSource.ToString();
            FullyQualifiedNamespace = args.FullyQualifiedNamespace;
        }

        public Subscription Subscription { get; }
        
        public Exception Exception { get; }

        [Obsolete("The Azure SDK no longer provides this information")]
        public string Action { get; } = "";

        [Obsolete("The Azure SDK no longer provides this information")]
        public string ClientId { get; } = "";

        [Obsolete("The Azure SDK no longer provides this information")]
        public string Endpoint { get; } = "";
        
        public string EntityPath { get; }
        
        public string ErrorSource { get; }
        
        public string FullyQualifiedNamespace { get; }
    }
}
