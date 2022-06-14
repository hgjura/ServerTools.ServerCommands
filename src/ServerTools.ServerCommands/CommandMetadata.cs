using System;
using System.Collections.Generic;

namespace ServerTools.ServerCommands
{
    public struct CommandMetadata
    {
        public Guid? UniqueId;
        public string CommandType;
        public DateTime? CommandPostedOn;
        public DateTime? CommandLastExecutedOn;
        public DateTime? CommandCompletedOn;
        public DateTime? CommandLastAttemptedAndFailedOn;

        public DateTime? CommandRespondedOn;

        public Guid? CommandDeadletterQueueUniqueId;
        public string CommandDeadletterQueueErrorMessage;
        public string CommandDeadletterQueueInnerErrorMesssage;
        public DateTime? CommandDeadletterQueuePostedOn;
        public long CommandDeadletterQueueRetryCount;

        public Guid? ResponseUniqueId;
        public string ResponseType;
        public DateTime? ResponsePostedOn;
        public DateTime? ResponseLastExecutedOn;
        public DateTime? ResponseCompletedOn;
        public DateTime? ResponseLastAttemptedAndFailedOn;

        public Guid? ResponseDeadletterQueueUniqueId;
        public string ResponseDeadletterQueueErrorMessage;
        public string ResponseDeadletterQueueInnerErrorMesssage;
        public DateTime? ResponseDeadletterQueuePostedOn;
        public long ResponseDeadletterQueueRetryCount;

        public bool BatchIsCorrelated;
        public Guid? BatchCorrelationId;
        public int BatchOrderId;
        public bool BatchIsLast;

        public Dictionary<string, object> CustomMetadata => new Dictionary<string, object>();
    }
}
