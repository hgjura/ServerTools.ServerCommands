using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands
{
    public static class StringExtensions
    {
        private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        private static readonly Regex base64 = new Regex("^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{2}==)?$", RegexOptions);

        public static string NullIfEmptyOrWhitespace(this string s)
        {
            return (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)) ? null : s;
        }

        public static bool IsBase64(this string s)
        {
            return base64.IsMatch(s);
        }

    }

    public static class CommandMetadataExtensions
    {
        public static void CommandStartNew(this ref CommandMetadata s, string CommandType)
        {
            s.UniqueId = Guid.NewGuid();
            s.CommandType = CommandType;
            s.CommandPostedOn = DateTime.UtcNow;
            s.CustomMetadata = new Dictionary<string, object>();
        }

        public static void CommandExeuted(this ref CommandMetadata s)
        {
            s.CommandLastExecutedOn = DateTime.UtcNow;
        }
        public static void CommandCompleted(this ref CommandMetadata s)
        {
            s.CommandCompletedOn = DateTime.UtcNow;

        }
        public static void CommandFailed(this ref CommandMetadata s)
        {
            s.CommandLastAttemptedAndFailedOn = DateTime.UtcNow;
        }
        public static void CommandSentToDlq(this ref CommandMetadata s, Exception ex)
        {
            if (s.CommandDeadletterQueueUniqueId == null)
            {
                s.CommandDeadletterQueueUniqueId = Guid.NewGuid();
                s.CommandDeadletterQueuePostedOn = DateTime.UtcNow;
            }
            s.CommandDeadletterQueueErrorMessage = ex.Message;
            s.CommandDeadletterQueueInnerErrorMesssage = ex.InnerException?.Message;
        }
        public static void CommandResubmitFromDLQ(this ref CommandMetadata s)
        {
            s.CustomMetadata.Add("ResubmittedFromDlqOn", DateTime.UtcNow);
        }


        public static void ResponseStartNew(this ref CommandMetadata s, string ResponseType)
        {
            s.ResponseUniqueId = Guid.NewGuid();
            s.ResponseType = ResponseType;
            s.ResponsePostedOn = DateTime.UtcNow;
        }

        public static void ResponseExeuted(this ref CommandMetadata s)
        {
            s.ResponseLastExecutedOn = DateTime.UtcNow;
        }
        public static void ResponseCompleted(this ref CommandMetadata s)
        {
            s.ResponseCompletedOn = DateTime.UtcNow;
        }
        public static void ResponseFailed(this ref CommandMetadata s)
        {
            s.ResponseLastAttemptedAndFailedOn = DateTime.UtcNow;
        }
        public static void ResponseSentToDlq(this ref CommandMetadata s, Exception ex)
        {
            if (s.ResponseDeadletterQueueUniqueId == null)
            {
                s.ResponseDeadletterQueueUniqueId = Guid.NewGuid();
                s.ResponseDeadletterQueuePostedOn = DateTime.UtcNow;
            }
            s.ResponseDeadletterQueueErrorMessage = ex.Message;
            s.ResponseDeadletterQueueInnerErrorMesssage = ex.InnerException?.Message;
           
            s.ResponseDeadletterQueueRetryCount += 1;
        }
        public static void ResponseResubmitFromDLQ(this ref CommandMetadata s)
        {
            s.CustomMetadata.Add("ResponseResubmittedFromDlqOn", DateTime.UtcNow);
        }


    }
}
