using System;
using System.Text.RegularExpressions;

namespace EL.ServiceBus
{
    public class Subscription
    {
        public readonly Topic Topic;
        public string SubscriptionName { get; protected set; }
        protected string fullName;
        internal static string Pattern = "^[a-z]+[a-z-]*[a-z]+$";
        private static Regex regex = new Regex(Pattern, RegexOptions.Compiled);

        public Subscription(Topic topic, string productContext, string process)
        {
            if (topic == null)
            {
                throw new ArgumentException("Topic may not be null", nameof(topic));
            }
            if (!regex.IsMatch(productContext ?? ""))
            {
                throw new ArgumentException("Product Context name must match pattern: " + Pattern, nameof(productContext));
            }
            if (!regex.IsMatch(process ?? ""))
            {
                throw new ArgumentException("Process name must match pattern: " + Pattern, nameof(process));
            }

            this.Topic = topic;
            SubscriptionName = $"{productContext}.{process}";
            fullName = $"{Topic}=>{SubscriptionName}";
        }

        public override string ToString() => fullName;
    }

    public class DeadLetterSubscription : Subscription
    {
        private const string DeadLetterQueueSuffix = "/$DeadLetterQueue";

        public DeadLetterSubscription(Topic topic, string productContext, string process) : base (topic, productContext, process)
        {
            SubscriptionName += DeadLetterQueueSuffix;
            fullName += DeadLetterQueueSuffix;
        }
    }
}
