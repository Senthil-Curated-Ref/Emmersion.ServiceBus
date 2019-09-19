using System;
using System.Text.RegularExpressions;

namespace El.ServiceBus
{
    public class MessageEvent
    {
        private readonly string nameAndVersion;
        private static string pattern = "^[a-z]+[a-z.-]*[a-z]+$";
        private static Regex regex = new Regex(pattern, RegexOptions.Compiled);
        public MessageEvent(string eventName, int version)
        {
            if (!regex.IsMatch(eventName))
            {
                throw new ArgumentException("Event name must match pattern: " + pattern, nameof(eventName));
            }
            if (version < 0)
            {
                throw new ArgumentException("Event version may not be negative", nameof(version));
            }
            nameAndVersion = $"{eventName}.v{version}";
        }

        public override string ToString() => nameAndVersion;
    }
}
