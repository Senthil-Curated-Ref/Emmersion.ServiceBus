using System;

namespace EL.ServiceBus
{
    public delegate void OnUnhandledException(object source, UnhandledExceptionArgs args);

    public class UnhandledExceptionArgs
    {
        public UnhandledExceptionArgs(string messageEvent, Exception exception)
        {
            MessageEvent = messageEvent;
            UnhandledException = exception;
        }

        public string MessageEvent { get; }
        public Exception UnhandledException { get; }
    }
}
