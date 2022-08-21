using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Polly;
using System;

namespace ServerTools.ServerCommands.AzureServiceBus
{
    public class AzureServiceBusConnectionOptions : ConnectionOptions
    {
        public string ConnectionString { get; }
        public AzureServiceBusTier Tier { get; }
        public string QueueNamePrefix { get; }
        public int MaxMessagesToRetrieve { get; }
        public TimeSpan? MaxWaitTime { get; }
        public TimeSpan DefaultMessageTimeToLive { get; }
        public TimeSpan LockDuration { get; }
        public bool EnableBatchedOperations { get; }
        public bool DeadLetteringOnMessageExpiration { get; }
        public int MaxDeliveryCount { get; }
        public long MaxSizeInMegabytes { get; }
        public TimeSpan AutoDeleteOnIdle { get; }
        public bool RequiresDuplicateDetection { get; }
        public bool RequiresSession { get; }

        public AzureServiceBusConnectionOptions(string ConnectionString, AzureServiceBusTier Tier = AzureServiceBusTier.Basic, int MaxDequeueCountForError = 5, ILogger Log = null, AsyncPolicy RetryPolicy = null, string QueueNamePrefix = null, int MaxMessagesToRetrieve = 32, TimeSpan? MaxWaitTime = null, TimeSpan DefaultMessageTimeToLive = default, TimeSpan LockDuration = default, bool EnableBatchedOperations = true, bool DeadLetteringOnMessageExpiration = true, int MaxDeliveryCount = 8, long MaxSizeInMegabytes = 2048, TimeSpan AutoDeleteOnIdle = default, bool RequiresDuplicateDetection = true, bool RequiresSession = false) : base(Log, RetryPolicy, MaxDequeueCountForError)
        {
            AzureServiceBusValidators.ValidateTierAutoDeleteOnIdle(Tier, AutoDeleteOnIdle);
            AzureServiceBusValidators.ValidateTierRequiresDuplicateDetection(Tier, RequiresDuplicateDetection);
            AzureServiceBusValidators.ValidateTierRequiresSession(Tier, RequiresSession);

            this.ConnectionString = ConnectionString;
            this.Tier = Tier;
            this.QueueNamePrefix = QueueNamePrefix;
            this.MaxMessagesToRetrieve = MaxMessagesToRetrieve;
            this.MaxWaitTime = MaxWaitTime;
            this.DefaultMessageTimeToLive = DefaultMessageTimeToLive == default ? TimeSpan.FromMinutes(30) : DefaultMessageTimeToLive;
            this.LockDuration = LockDuration == default ? TimeSpan.FromSeconds(45) : LockDuration;
            this.EnableBatchedOperations = EnableBatchedOperations;
            this.DeadLetteringOnMessageExpiration = DeadLetteringOnMessageExpiration;
            this.MaxDeliveryCount = MaxDeliveryCount;
            this.MaxSizeInMegabytes = MaxSizeInMegabytes;
            this.AutoDeleteOnIdle = AutoDeleteOnIdle == default ? TimeSpan.FromDays(7) : AutoDeleteOnIdle;
            this.RequiresDuplicateDetection = RequiresDuplicateDetection;
            this.RequiresSession = RequiresSession;
        }
    }

    public enum AzureServiceBusTier
    {
        Basic,
        Standard,
        Premium
    }
}
