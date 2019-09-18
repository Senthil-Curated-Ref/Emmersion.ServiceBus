using Microsoft.Azure.WebJobs;

namespace El.ServiceBus
{
    public class MessageTrigger
    {
        private readonly IMessageSubscriber messageSubscriber;

        public MessageTrigger(IMessageSubscriber messageSubscriber)
        {
            this.messageSubscriber = messageSubscriber;
        }

        public void HandleMessage([ServiceBusTrigger("%topic-name%", "%subscriber-name%")] string serializedMessage)
        {
            messageSubscriber.RouteMessage(serializedMessage);
        }
    }
}
