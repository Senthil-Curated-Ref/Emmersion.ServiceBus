using System;
using System.Text.RegularExpressions;

namespace EL.ServiceBus
{
    public class Topic
    {
        private readonly string fullName;
        internal static string Pattern = "^[a-z]+[a-z-]*[a-z]+$";
        private static Regex regex = new Regex(Pattern, RegexOptions.Compiled);
        private const int AzureTopicNameMaximumLength = 260;

        public Topic(string productContext, string eventName, int version)
        {
            if (!regex.IsMatch(productContext ?? ""))
            {
                throw new ArgumentException("Product Context name must match pattern: " + Pattern, nameof(productContext));
            }
            if (!regex.IsMatch(eventName ?? ""))
            {
                throw new ArgumentException("Event name must match pattern: " + Pattern, nameof(eventName));
            }
            if (version < 0)
            {
                throw new ArgumentException("Version may not be negative", nameof(version));
            }

            fullName = $"{productContext}.{eventName}.v{version}";
            
            if (fullName.Length > AzureTopicNameMaximumLength) {
                throw new Exception($"The topic name '{fullName}' exceeds the Azure {AzureTopicNameMaximumLength} character limit");
            }
        }

        public override string ToString() => fullName;
    }
}
