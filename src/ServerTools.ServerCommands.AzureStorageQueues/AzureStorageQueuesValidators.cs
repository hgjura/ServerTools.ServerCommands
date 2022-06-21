using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ServerTools.ServerCommands.AzureStorageQueues
{
    public static class AzureStorageQueuesValidators
    {
        private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        private static readonly Regex QueueRegex = new Regex("^[a-z0-9]{1}([-a-z0-9]{1,60})[a-z0-9]{1}$", RegexOptions);
        private static readonly Regex StorageRegex = new Regex("^[a-z0-9]{3,24}$", RegexOptions);

        public static void ValidateNameForAzureStorage(string accountName)
        {
            if (accountName.NullIfEmptyOrWhitespace() == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, StringResponses.NullOrEmptyOrWhitespaceStringArgument, accountName));
            }

            if (!StorageRegex.IsMatch(accountName))
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, StringResponses.InvalidResourceName, AzureQueuesStringResponses.Storage, accountName));
            }
        }

        public static void ValidateNameForAzureStorageQueue(string queueName)
        {
            if (queueName.NullIfEmptyOrWhitespace() == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, StringResponses.NullOrEmptyOrWhitespaceStringArgument, queueName));
            }

            if (!QueueRegex.IsMatch(queueName) || queueName.Contains("--"))
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, StringResponses.InvalidResourceName, AzureQueuesStringResponses.Queue, queueName));
            }
        }

        public static void ValidateNameForAzureStorageAccountKey(string accountKey)
        {
            if (accountKey.NullIfEmptyOrWhitespace() == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, StringResponses.NullOrEmptyOrWhitespaceStringArgument, accountKey));
            }

            if (!accountKey.IsBase64())
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, AzureQueuesStringResponses.InvalidAccountKey, accountKey));
            }
        }
    }
}
