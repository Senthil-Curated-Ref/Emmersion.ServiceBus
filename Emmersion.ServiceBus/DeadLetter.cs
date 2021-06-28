namespace Emmersion.ServiceBus
{
    public class DeadLetter
    {
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public string Body { get; set; }
    }
}
