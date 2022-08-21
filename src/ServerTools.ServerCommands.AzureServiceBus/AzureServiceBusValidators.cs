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

        public static void ValidateTierAutoDeleteOnIdle(AzureServiceBusTier Tier, TimeSpan AutoDeleteOnIdle)
        {
            if (Tier == AzureServiceBusTier.Basic && AutoDeleteOnIdle != default)
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, AzureServiceBusStringResponses.TierBasicCannotHaveThisFeature, "AutoDeleteOnIdle"));
            }
        }

        public static void ValidateTierRequiresSession(AzureServiceBusTier Tier, bool RequiresSession)
        {
            if (Tier == AzureServiceBusTier.Basic && RequiresSession)
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, AzureServiceBusStringResponses.TierBasicCannotHaveThisFeature, "RequiresSession"));
            }
        }
        public static void ValidateTierRequiresDuplicateDetection(AzureServiceBusTier Tier, bool RequiresDuplicateDetection)
        {
            if (Tier == AzureServiceBusTier.Basic && RequiresDuplicateDetection)
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, AzureServiceBusStringResponses.TierBasicCannotHaveThisFeature, "RequiresDuplicateDetection"));
            }
        }

        public static void ValidateOrderedMessageFeature(bool RequiresSession)
        {
            if (!RequiresSession)
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, AzureServiceBusStringResponses.FeatureNotSupportedWhenSessionIsOff, "OrderedMessage"));
            }
        }
    }
}
