using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ServerTools.ServerCommands.AzureServiceBus
{
    public static class AzureServiceBusValidators
    {
        private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        private static readonly Regex QueueRegex = new Regex("^[a-z0-9]{1}([-a-z0-9]{1,60})[a-z0-9]{1}$", RegexOptions);

        public static void ValidateNameForAzureServiceBusQueue(string queueName)
        {
            if (queueName.NullIfEmptyOrWhitespace() == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, StringResponses.NullOrEmptyOrWhitespaceStringArgument, queueName));
            }

            if (!QueueRegex.IsMatch(queueName) || queueName.Contains("--"))
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, StringResponses.InvalidResourceName, AzureServiceBusStringResponses.Queue, queueName));
            }
        }
    }
}
