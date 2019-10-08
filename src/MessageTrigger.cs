using Microsoft.Azure.WebJobs;

namespace EL.ServiceBus
{
    public class MessageTrigger
    {
        private readonly IMessageSubscriber messageSubscriber;

        public MessageTrigger(IMessageSubscriber messageSubscriber)
        {
            this.messageSubscriber = messageSubscriber;
        }

        public void HandleMessage([ServiceBusTrigger("%ELServiceBusTopicName%", "%ELServiceBusSubscriberName%", Connection = "ELServiceBus")] string serializedMessage)
        {
            messageSubscriber.RouteMessage(serializedMessage);
        }
    }
}
