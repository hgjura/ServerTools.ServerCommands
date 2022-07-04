using Microsoft.Extensions.Logging;
using Polly;
using System;

namespace ServerTools.ServerCommands.AzureServiceBus
{
    public class AzureServiceBusConnectionOptions : ConnectionOptions
    {
        public string ConnectionString { get; }
        public string QueueNamePrefix { get; }
        public int MaxMessagesToRetrieve { get; }
        public TimeSpan? MaxWaitTime { get; }

        public AzureServiceBusConnectionOptions(string ConnectionString, int MaxDequeueCountForError = 5, ILogger Log = null, AsyncPolicy RetryPolicy = null, string QueueNamePrefix = null, int MaxMessagesToRetrieve = 32, TimeSpan? MaxWaitTime = null) : base(Log, RetryPolicy, MaxDequeueCountForError)
        {
            this.ConnectionString = ConnectionString;
            this.QueueNamePrefix = QueueNamePrefix;
            this.MaxMessagesToRetrieve = MaxMessagesToRetrieve;
            this.MaxWaitTime = MaxWaitTime;
        }
    }
}
