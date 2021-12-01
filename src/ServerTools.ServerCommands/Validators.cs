using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands
{
    public static class Validators
    {
        private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        private static readonly Regex QueueRegex = new Regex("^[a-z0-9]{1}([-a-z0-9]{1,60})[a-z0-9]{1}$", RegexOptions);
        private static readonly Regex StorageRegex = new Regex("^[a-z0-9]{3,24}$", RegexOptions);

        public static void ValidateContainer(CommandContainer commandContainer)
        {
            if(commandContainer == null)
                throw new ArgumentNullException(StringResponses.NullContainer);
        }

        public static void ValidateNameForAzureStorage(string accountName)
        {
            if (accountName.NullIfEmptyOrWhitespace() == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, StringResponses.NullOrEmptyOrWhitespaceStringArgument, accountName));
            }

            if (!StorageRegex.IsMatch(accountName))
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, StringResponses.InvalidResourceName, StringResponses.Storage, accountName));
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
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, StringResponses.InvalidResourceName, StringResponses.Queue, queueName));
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
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, StringResponses.InvalidAccountKey, accountKey));
            }
        }
    }
}
