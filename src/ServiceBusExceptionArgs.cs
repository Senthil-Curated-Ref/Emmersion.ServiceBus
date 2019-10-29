using System;
using Microsoft.Azure.ServiceBus;

namespace EL.ServiceBus
{
    public delegate void OnServiceBusException(object source, ServiceBusExceptionArgs args);

    public class ServiceBusExceptionArgs
    {
        public ServiceBusExceptionArgs(Exception exception, string action, string clientId, string endpoint, string entityPath)
        {
            Exception = exception;
            Action = action;
            ClientId = clientId;
            Endpoint = endpoint;
            EntityPath = entityPath;
        }

        internal ServiceBusExceptionArgs(ExceptionReceivedEventArgs args)
        {
            Exception = args.Exception;
            Action = args.ExceptionReceivedContext?.Action ?? "";
            ClientId = args.ExceptionReceivedContext?.ClientId ?? "";
            Endpoint = args.ExceptionReceivedContext?.Endpoint ?? "";
            EntityPath = args.ExceptionReceivedContext?.EntityPath ?? "";
        }

        public Exception Exception { get; }
        public string Action { get; }
        public string ClientId { get; }
        public string Endpoint { get; }
        public string EntityPath { get; }
    }
}
