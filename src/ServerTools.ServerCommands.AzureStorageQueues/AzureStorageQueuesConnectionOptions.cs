﻿using Microsoft.Extensions.Logging;
using Polly;
using System;

namespace ServerTools.ServerCommands.AzureStorageQueues
{
    public class AzureStorageQueuesConnectionOptions : ConnectionOptions
    {
        public readonly string AccountName;
        public readonly string AccountKey;

        public readonly string QueueNamePrefix = null;
        public int? MaxMessagesToRetrieve { get; }
        public TimeSpan? VisibilityTimeout { get; }
        public AzureStorageQueuesConnectionOptions(string AccountName, string AccountKey, int MaxDequeueCountForError = 5, ILogger Log = null, AsyncPolicy RetryPolicy = null, string QueueNamePrefix = null, int MaxMessagesToRetrieve = 32, TimeSpan? VisibilityTimeout = null) : base(Log, RetryPolicy, MaxDequeueCountForError)
        {
            AzureStorageQueuesValidators.ValidateNameForAzureStorage(AccountName);
            AzureStorageQueuesValidators.ValidateNameForAzureStorageAccountKey(AccountKey);

            this.AccountName = AccountName;
            this.AccountKey = AccountKey;   

            this.QueueNamePrefix = QueueNamePrefix;

            this.MaxMessagesToRetrieve = MaxMessagesToRetrieve;
            this.VisibilityTimeout = VisibilityTimeout;
        }
    }
}
