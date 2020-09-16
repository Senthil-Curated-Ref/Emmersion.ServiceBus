using System;
using System.Text.RegularExpressions;

namespace EL.ServiceBus
{
    public class Subscription
    {
        public readonly Topic Topic;
        public readonly string SubscriptionName;
        readonly string fullName;
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

            Topic = topic;
            SubscriptionName = $"{productContext}.{process}";
            fullName = $"{Topic}=>{SubscriptionName}";
        }

        private Subscription(Topic topic, string subscriptionName)
        {
            Topic = topic;
            SubscriptionName = subscriptionName;
            fullName = $"{Topic}=>{SubscriptionName}";
        }

        public override string ToString() => fullName;
    }
}
