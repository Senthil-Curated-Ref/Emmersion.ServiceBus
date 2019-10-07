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

        public void HandleMessage([ServiceBusTrigger("%el-service-bus-topic-name%", "%el-service-bus-subscriber-name%", Connection = "el-service-bus")] string serializedMessage)
        {
            messageSubscriber.RouteMessage(serializedMessage);
        }
    }
}
